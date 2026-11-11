// Tests of the license check: node --test .github/scripts/check-licenses.test.mjs
//
// The npm fixtures are created at runtime in a temporary directory and removed afterwards.

import assert from 'node:assert/strict'
import { mkdirSync, mkdtempSync, rmSync, writeFileSync } from 'node:fs'
import os from 'node:os'
import path from 'node:path'
import { after, before, describe, test } from 'node:test'
import {
    checkNpmProject,
    defaultPolicyFile,
    ensureNpmPackages,
    loadPolicy,
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

function byName(results) {
    return Object.fromEntries(results.map((result) => [result.name, result.approved]))
}

before(() => {
    temporaryDirectory = mkdtempSync(path.join(os.tmpdir(), 'wavemp-licenses-'))
})

after(() => {
    rmSync(temporaryDirectory, { recursive: true, force: true })
})

describe('license policy', () => {
    test('loads and is valid', () => {
        const policy = loadPolicy()

        assert.ok(policy.allowedLicenses.includes('MIT'))
        assert.deepEqual(policy.npm.projects, ['source/WaveMP.Web', 'tests/WaveMP.E2E'])
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

    test('rejects an incomplete policy', () => {
        const file = path.join(temporaryDirectory, 'policy-incomplete.json')
        writeJson(file, { allowedLicenses: ['MIT'] })

        assert.throws(() => loadPolicy(file), /nuget\.solution/)
    })
})

describe('npm license check', () => {
    before(() => {
        ensureNpmPackages(scriptsDirectory)
    })

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
})
