// Tests of the license check: node --test .github/scripts/check-licenses.test.mjs
//
// The npm and NuGet fixtures are created at runtime in a temporary directory and removed
// afterwards.

import assert from 'node:assert/strict'
import { mkdirSync, mkdtempSync, rmSync, writeFileSync } from 'node:fs'
import os from 'node:os'
import path from 'node:path'
import { after, before, describe, test } from 'node:test'
import {
    checkDotnetTools,
    checkNpmProject,
    defaultPolicyFile,
    ensureNpmPackages,
    findUncoveredProjects,
    listProjectFiles,
    loadPolicy,
    readSolutionProjects,
    scriptsDirectory,
} from './check-licenses.mjs'

let temporaryDirectory

function writeJson(file, content) {
    mkdirSync(path.dirname(file), { recursive: true })
    writeFileSync(file, `${JSON.stringify(content, null, 2)}\n`)
}

/**
 * Creates an npm project with installed packages. Each package has a name and the license
 * metadata to test ("license", "licenses" or none).
 */
function createProject(name, packages) {
    const directory = path.join(temporaryDirectory, name)
    writeJson(path.join(directory, 'package.json'), {
        name,
        version: '1.0.0',
        private: true,
        dependencies: Object.fromEntries(packages.map((item) => [item.name, '1.0.0'])),
    })
    for (const item of packages) {
        writeJson(path.join(directory, 'node_modules', item.name, 'package.json'), {
            version: '1.0.0',
            ...item,
        })
    }

    return directory
}

/**
 * Creates a .NET tool manifest and the restored tool packages (nuspec files) in a NuGet packages
 * folder. Each tool has an id and the license element of its nuspec (or none). Package and version
 * folders are lowercase like in the NuGet packages folder. The nuspec file name is lowercase as
 * well, unless the tool sets originalCase: then it keeps the case of the id, like
 * `dotnet tool restore` does.
 */
function createTools(name, tools) {
    const directory = path.join(temporaryDirectory, name)
    const packagesDirectory = path.join(directory, 'packages')
    const manifest = path.join(directory, 'dotnet-tools.json')
    writeJson(manifest, {
        version: 1,
        isRoot: true,
        tools: Object.fromEntries(
            tools.map((tool) => [tool.id.toLowerCase(), { version: '1.0.0', commands: [] }]),
        ),
    })
    for (const tool of tools) {
        const id = tool.id.toLowerCase()
        const fileName = `${tool.originalCase ? tool.id : id}.nuspec`
        const nuspec = path.join(packagesDirectory, id, '1.0.0', fileName)
        mkdirSync(path.dirname(nuspec), { recursive: true })
        writeFileSync(
            nuspec,
            [
                '<?xml version="1.0" encoding="utf-8"?>',
                '<package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd">',
                '  <metadata>',
                `    <id>${tool.id}</id>`,
                '    <version>1.0.0</version>',
                tool.license ? `    ${tool.license}` : '',
                '  </metadata>',
                '</package>',
                '',
            ].join('\n'),
        )
    }

    return { manifest, packagesDirectory }
}

function withToolManifest(policy, manifest, allowedPackages = []) {
    return { ...policy, nuget: { ...policy.nuget, toolManifests: [manifest], allowedPackages } }
}

function expression(license) {
    return `<license type="expression">${license}</license>`
}

function byName(results) {
    return Object.fromEntries(results.map((result) => [result.name, result.approved]))
}

before(() => {
    temporaryDirectory = mkdtempSync(path.join(os.tmpdir(), 'wavemp-licenses-'))
    ensureNpmPackages(scriptsDirectory)
})

after(() => {
    rmSync(temporaryDirectory, { recursive: true, force: true })
})

describe('license policy', () => {
    test('loads and is valid', () => {
        const policy = loadPolicy()

        assert.ok(policy.allowedLicenses.includes('MIT'))
        assert.equal(policy.nuget.solution, 'WaveMP.slnx')
        assert.deepEqual(policy.nuget.toolManifests, ['dotnet-tools.json'])
        assert.deepEqual(policy.npm.projects, [
            '.github/scripts',
            'source/WaveMP.Web',
            'tests/WaveMP.E2E',
        ])
    })

    test('allows no GPL, AGPL or LGPL license', () => {
        const policy = loadPolicy()

        assert.deepEqual(
            policy.allowedLicenses.filter((id) => /GPL/i.test(id)),
            [],
        )
    })

    test('rejects a policy that allows a GPL license', () => {
        const policy = loadPolicy(defaultPolicyFile)
        const file = path.join(temporaryDirectory, 'policy-with-gpl.json')
        writeJson(file, {
            ...policy,
            allowedLicenses: [...policy.allowedLicenses, 'AGPL-3.0-only'],
        })

        assert.throws(() => loadPolicy(file), /must not contain GPL, AGPL or LGPL/)
    })

    test('rejects npm package exceptions without an exact version', () => {
        const policy = loadPolicy(defaultPolicyFile)
        const file = path.join(temporaryDirectory, 'policy-with-range.json')
        writeJson(file, {
            ...policy,
            npm: { ...policy.npm, allowedPackages: { reviewed: '^1.0.0', other: '1.0.0' } },
        })

        assert.throws(() => loadPolicy(file), /must contain exact versions \(reviewed@\^1\.0\.0\)/)
    })

    test('rejects an incomplete policy', () => {
        const file = path.join(temporaryDirectory, 'policy-incomplete.json')
        writeJson(file, { allowedLicenses: ['MIT'] })

        assert.throws(() => loadPolicy(file), /nuget\.solution.*nuget\.toolManifests/)
    })
})

describe('project coverage', () => {
    test('covers every project of the repository', () => {
        const policy = loadPolicy()

        const problems = findUncoveredProjects(
            policy,
            listProjectFiles(),
            readSolutionProjects(policy.nuget.solution),
        )

        assert.deepEqual(problems, [])
    })

    test('finds every project of the repository', () => {
        const files = listProjectFiles()

        assert.ok(files.includes('dotnet-tools.json'))
        assert.ok(files.includes('.github/scripts/package.json'))
        assert.ok(files.includes('source/WaveMP.Service/WaveMP.Service.csproj'))
        assert.ok(files.every((file) => !file.includes('node_modules')))
    })

    test('reports projects that the policy or the solution does not cover', () => {
        const policy = {
            nuget: { solution: 'App.slnx', toolManifests: ['dotnet-tools.json'] },
            npm: { projects: ['source/Web/', '.github/scripts'] },
        }
        const files = [
            'source/App/App.csproj',
            'source/New/New.csproj',
            'dotnet-tools.json',
            '.config/dotnet-tools.json',
            'source/Web/package.json',
            '.github/scripts/package.json',
            'tools/New/package.json',
            'package.json',
        ]

        const problems = findUncoveredProjects(policy, files, ['source\\App\\App.csproj'])

        assert.deepEqual(problems, [
            '.NET project source/New/New.csproj is not part of App.slnx',
            'tool manifest .config/dotnet-tools.json is not listed in "nuget.toolManifests"',
            'npm project tools/New is not listed in "npm.projects"',
            'npm project . is not listed in "npm.projects"',
        ])
    })
})

describe('npm license check', () => {
    test('approves permissive and dual licenses', async () => {
        const directory = createProject('permissive', [
            { name: 'mit', license: 'MIT' },
            { name: 'dual-gpl', license: '(MIT OR GPL-3.0-only)' },
            { name: 'legacy-mit', licenses: [{ type: 'MIT' }] },
        ])

        const results = await checkNpmProject(directory, loadPolicy())

        assert.deepEqual(byName(results), { 'dual-gpl': true, 'legacy-mit': true, mit: true })
    })

    test('rejects GPL, AGPL, combined and missing licenses', async () => {
        const directory = createProject('copyleft', [
            { name: 'gpl', license: 'GPL-3.0-only' },
            { name: 'agpl', license: 'AGPL-3.0-only' },
            { name: 'lgpl', license: 'LGPL-2.1-only' },
            { name: 'combined', license: '(MIT AND GPL-2.0-only)' },
            { name: 'unlicensed' },
        ])

        const results = await checkNpmProject(directory, loadPolicy())

        assert.deepEqual(byName(results), {
            agpl: false,
            combined: false,
            gpl: false,
            lgpl: false,
            unlicensed: false,
        })
    })

    test('approves reviewed package exceptions', async () => {
        const directory = createProject('exceptions', [
            { name: 'reviewed-gpl', license: 'GPL-3.0-only' },
            { name: 'other-gpl', license: 'GPL-3.0-only' },
        ])
        const policy = loadPolicy()
        const policyWithException = {
            ...policy,
            npm: { ...policy.npm, allowedPackages: { 'reviewed-gpl': '1.0.0' } },
        }

        const results = await checkNpmProject(directory, policyWithException)

        assert.deepEqual(byName(results), { 'other-gpl': false, 'reviewed-gpl': true })
    })

    test('approves the reviewed exceptions only in their exact version', async () => {
        const directory = createProject('pinned-exceptions', [
            { name: 'spdx-exceptions', license: 'CC-BY-3.0' },
        ])
        const policy = loadPolicy()
        assert.notEqual(policy.npm.allowedPackages['spdx-exceptions'], '1.0.0')

        const results = await checkNpmProject(directory, policy)

        assert.deepEqual(byName(results), { 'spdx-exceptions': false })
    })
})

describe('.NET tool license check', () => {
    test('approves tools with an allowed license expression', () => {
        const { manifest, packagesDirectory } = createTools('tools-permissive', [
            { id: 'Mit.Tool', license: expression('MIT') },
            { id: 'Apache.Tool', license: expression('Apache-2.0') },
            { id: 'Dual.Tool', license: expression('MIT OR GPL-3.0-only') },
        ])

        const results = checkDotnetTools(
            withToolManifest(loadPolicy(), manifest),
            packagesDirectory,
        )

        assert.deepEqual(byName(results), {
            'apache.tool': true,
            'dual.tool': true,
            'mit.tool': true,
        })
    })

    test('rejects GPL, AGPL, combined and missing license expressions', () => {
        const { manifest, packagesDirectory } = createTools('tools-copyleft', [
            { id: 'Gpl.Tool', license: expression('GPL-3.0-only') },
            { id: 'Agpl.Tool', license: expression('AGPL-3.0-or-later') },
            { id: 'Combined.Tool', license: expression('MIT AND GPL-2.0-only') },
            { id: 'File.Tool', license: '<license type="file">LICENSE.txt</license>' },
            { id: 'Unlicensed.Tool' },
        ])

        const results = checkDotnetTools(
            withToolManifest(loadPolicy(), manifest),
            packagesDirectory,
        )

        assert.deepEqual(byName(results), {
            'agpl.tool': false,
            'combined.tool': false,
            'file.tool': false,
            'gpl.tool': false,
            'unlicensed.tool': false,
        })
    })

    test('reads nuspec files that keep the case of the package ID', () => {
        const { manifest, packagesDirectory } = createTools('tools-original-case', [
            { id: 'Microsoft.OpenApi.Kiota', license: expression('MIT'), originalCase: true },
            { id: 'Mixed.Gpl.Tool', license: expression('GPL-3.0-only'), originalCase: true },
            { id: 'lower-case-tool', license: expression('Apache-2.0') },
        ])

        const results = checkDotnetTools(
            withToolManifest(loadPolicy(), manifest),
            packagesDirectory,
        )

        assert.deepEqual(
            Object.fromEntries(results.map((result) => [result.name, result.license])),
            {
                'lower-case-tool': 'Apache-2.0',
                'microsoft.openapi.kiota': 'MIT',
                'mixed.gpl.tool': 'GPL-3.0-only',
            },
        )
        assert.deepEqual(byName(results), {
            'lower-case-tool': true,
            'microsoft.openapi.kiota': true,
            'mixed.gpl.tool': false,
        })
    })

    test('approves reviewed package exceptions', () => {
        const { manifest, packagesDirectory } = createTools('tools-exceptions', [
            { id: 'Reviewed.Tool', license: expression('GPL-3.0-only') },
            { id: 'Other.Tool', license: expression('GPL-3.0-only') },
        ])
        const policy = withToolManifest(loadPolicy(), manifest, ['Reviewed.*'])

        const results = checkDotnetTools(policy, packagesDirectory)

        assert.deepEqual(byName(results), { 'other.tool': false, 'reviewed.tool': true })
    })

    test('fails for tools that are not restored', () => {
        const { manifest } = createTools('tools-missing', [
            { id: 'Mit.Tool', license: expression('MIT') },
        ])
        const emptyPackagesDirectory = path.join(temporaryDirectory, 'tools-missing', 'empty')

        assert.throws(
            () =>
                checkDotnetTools(withToolManifest(loadPolicy(), manifest), emptyPackagesDirectory),
            /mit\.tool 1\.0\.0 is not restored/,
        )
    })
})
