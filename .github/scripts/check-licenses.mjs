// License check for all NuGet and npm dependencies of WaveMP, used locally and in CI.
//
// Usage (from any directory): node .github/scripts/check-licenses.mjs [--policy <file>]
//
// The allowed licenses (SPDX identifiers) and the checked projects are configured in
// .github/license-policy.json. NuGet packages of the solution are checked with nuget-license
// (local .NET tool), local .NET tools by the license expression of their package and npm
// packages with licensee. Missing tools and packages are restored automatically. The check also
// fails if the repository contains a .NET project outside the solution or a tool manifest or npm
// project that the policy does not list, so that no dependency is missed.
// Exceptions for single packages are only added to the policy after a manual license review.
// Exits with 0 if every dependency has an allowed license, otherwise with 1.

import { spawnSync } from 'node:child_process'
import { existsSync, mkdtempSync, readdirSync, readFileSync, rmSync, statSync } from 'node:fs'
import { createRequire } from 'node:module'
import os from 'node:os'
import path from 'node:path'
import { fileURLToPath } from 'node:url'
import { parseArgs } from 'node:util'

export const scriptsDirectory = path.dirname(fileURLToPath(import.meta.url))
export const repositoryRoot = path.resolve(scriptsDirectory, '..', '..')
export const defaultPolicyFile = path.join(repositoryRoot, '.github', 'license-policy.json')

// GPL, AGPL and LGPL must never be allowed globally (LGPL only as a reviewed package exception).
const forbiddenLicensePattern = /GPL/i
// npm exceptions name exact versions, so that every update of such a package is reviewed again.
const exactVersionPattern = /^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?(?:\+[0-9A-Za-z.-]+)?$/
const dotnetProjectPattern = /\.(?:cs|fs|vb)proj$/i

function isStringArray(value) {
    return Array.isArray(value) && value.every((item) => typeof item === 'string')
}

function isStringMap(value) {
    return (
        typeof value === 'object' &&
        value !== null &&
        !Array.isArray(value) &&
        Object.values(value).every((item) => typeof item === 'string')
    )
}

function relativeToRoot(file) {
    const relative = path.relative(repositoryRoot, file)
    if (relative.startsWith('..') || path.isAbsolute(relative)) {
        return file
    }

    return relative.replaceAll('\\', '/') || '.'
}

/**
 * Normalizes a path relative to the repository root to forward slashes without a trailing slash.
 */
function normalizeRelativePath(file) {
    return path.posix.normalize(file.replaceAll('\\', '/')).replace(/(?<=.)\/$/, '')
}

function requireScriptsPackage(name) {
    return createRequire(path.join(scriptsDirectory, 'package.json'))(name)
}

/**
 * Reads and validates the license policy. Throws an error with a clear message if it is invalid.
 */
export function loadPolicy(file = defaultPolicyFile) {
    let policy
    try {
        policy = JSON.parse(readFileSync(file, 'utf8'))
    } catch (error) {
        throw new Error(`Cannot read the license policy ${file}: ${error.message}`, {
            cause: error,
        })
    }

    const problems = []
    if (!isStringArray(policy?.allowedLicenses) || policy.allowedLicenses.length === 0) {
        problems.push('"allowedLicenses" must be a non-empty array of SPDX identifiers')
    } else {
        const forbidden = policy.allowedLicenses.filter((id) => forbiddenLicensePattern.test(id))
        if (forbidden.length > 0) {
            problems.push(
                `"allowedLicenses" must not contain GPL, AGPL or LGPL licenses (${forbidden.join(', ')})`,
            )
        }
    }
    if (typeof policy?.nuget?.solution !== 'string' || !policy.nuget.solution.endsWith('.slnx')) {
        problems.push('"nuget.solution" must be the path of the .slnx solution file')
    }
    if (!isStringArray(policy?.nuget?.toolManifests)) {
        problems.push('"nuget.toolManifests" must be an array of .NET tool manifests')
    }
    if (!isStringArray(policy?.nuget?.allowedPackages)) {
        problems.push('"nuget.allowedPackages" must be an array of package IDs')
    }
    if (!isStringArray(policy?.npm?.projects)) {
        problems.push('"npm.projects" must be an array of project directories')
    }
    if (!isStringMap(policy?.npm?.allowedPackages)) {
        problems.push('"npm.allowedPackages" must map package names to exact versions')
    } else {
        const ranges = Object.entries(policy.npm.allowedPackages).filter(
            ([, version]) => !exactVersionPattern.test(version),
        )
        if (ranges.length > 0) {
            const packages = ranges.map(([name, version]) => `${name}@${version}`).join(', ')
            problems.push(`"npm.allowedPackages" must contain exact versions (${packages})`)
        }
    }
    if (problems.length > 0) {
        throw new Error(`Invalid license policy ${file}: ${problems.join('; ')}`)
    }

    return policy
}

/**
 * Lists the .NET projects, .NET tool manifests and npm projects (package.json) of the repository,
 * relative to its root: tracked files and new files that git does not ignore.
 */
export function listProjectFiles() {
    const result = spawnSync(
        'git',
        [
            'ls-files',
            '-z',
            '--cached',
            '--others',
            '--exclude-standard',
            '--',
            '*.csproj',
            '*.fsproj',
            '*.vbproj',
            '*dotnet-tools.json',
            '*package.json',
        ],
        { cwd: repositoryRoot, encoding: 'utf8' },
    )
    if (result.error || result.status !== 0) {
        const reason = result.error?.message ?? result.stderr.trim()
        throw new Error(`Cannot list the project files of the repository with git: ${reason}`)
    }

    const files = result.stdout
        .split('\0')
        .filter((file) => file.length > 0 && existsSync(path.join(repositoryRoot, file)))

    return [...new Set(files)]
}

/**
 * Returns the project paths of a .slnx solution, relative to the repository root.
 */
export function readSolutionProjects(solution) {
    const file = path.resolve(repositoryRoot, solution)
    const solutionDirectory = path.relative(repositoryRoot, path.dirname(file))
    const content = readFileSync(file, 'utf8')

    return [...content.matchAll(/<Project\b[^>]*\bPath="([^"]+)"/g)].map((match) =>
        normalizeRelativePath(path.join(solutionDirectory, match[1])),
    )
}

/**
 * Returns a message for every project file the license check would miss: .NET projects outside
 * the solution and tool manifests or npm projects that the policy does not list.
 */
export function findUncoveredProjects(policy, files, solutionProjects) {
    const solution = new Set(solutionProjects.map(normalizeRelativePath))
    const toolManifests = new Set(policy.nuget.toolManifests.map(normalizeRelativePath))
    const npmProjects = new Set(policy.npm.projects.map(normalizeRelativePath))
    const problems = []
    for (const file of files.map(normalizeRelativePath)) {
        const name = path.posix.basename(file)
        if (dotnetProjectPattern.test(name) && !solution.has(file)) {
            problems.push(`.NET project ${file} is not part of ${policy.nuget.solution}`)
        } else if (name === 'dotnet-tools.json' && !toolManifests.has(file)) {
            problems.push(`tool manifest ${file} is not listed in "nuget.toolManifests"`)
        } else if (name === 'package.json' && !npmProjects.has(path.posix.dirname(file))) {
            problems.push(`npm project ${path.posix.dirname(file)} is not listed in "npm.projects"`)
        }
    }

    return problems
}

/**
 * Runs `npm ci` in the directory if its node_modules are missing or older than package-lock.json.
 */
export function ensureNpmPackages(directory) {
    const lockFile = path.join(directory, 'package-lock.json')
    const installedLockFile = path.join(directory, 'node_modules', '.package-lock.json')
    if (!existsSync(lockFile)) {
        throw new Error(`${lockFile} is missing`)
    }
    if (
        existsSync(installedLockFile) &&
        statSync(installedLockFile).mtimeMs >= statSync(lockFile).mtimeMs
    ) {
        return
    }

    console.log(`Installing npm packages in ${relativeToRoot(directory)}`)
    // A shell is needed to start npm.cmd on Windows; the command is a constant.
    const result = spawnSync('npm ci --no-audit --no-fund', {
        cwd: directory,
        stdio: 'inherit',
        shell: true,
    })
    if (result.error) {
        throw new Error(`Cannot run npm: ${result.error.message}`, { cause: result.error })
    }
    if (result.status !== 0) {
        throw new Error(`npm ci failed in ${relativeToRoot(directory)}`)
    }
}

/**
 * Checks the installed npm packages of a project. Resolves with one result per package
 * (name, version, license, approved).
 */
export function checkNpmProject(directory, policy) {
    const licensee = requireScriptsPackage('licensee')
    // licensee changes the configuration object, so every call gets a new one.
    const configuration = {
        licenses: { spdx: [...policy.allowedLicenses] },
        packages: { ...policy.npm.allowedPackages },
        // Fixes known legacy license metadata, e.g. a "licenses" array instead of "license".
        corrections: true,
    }

    return new Promise((resolve, reject) => {
        licensee(configuration, directory, (error, results) => {
            if (error) {
                reject(error)
            } else {
                resolve(results)
            }
        })
    })
}

/**
 * Returns whether an SPDX license expression is satisfied by the allowed licenses, with the
 * same rules as the npm check (OR needs one allowed license, AND needs all of them).
 */
export function isLicenseAllowed(expression, allowedLicenses) {
    const parse = requireScriptsPackage('spdx-expression-parse')
    const satisfiesAllowlist = requireScriptsPackage('spdx-whitelisted')
    let parsed
    try {
        parsed = parse(expression)
    } catch {
        return false
    }
    const allowlist = allowedLicenses.flatMap((id) => {
        try {
            const license = parse(id)
            return 'license' in license ? [license] : []
        } catch {
            return []
        }
    })

    return satisfiesAllowlist(parsed, allowlist)
}

function matchesPackagePattern(id, patterns) {
    return patterns.some((pattern) => {
        const source = pattern
            .split('*')
            .map((part) => part.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'))
            .join('.*')
        return new RegExp(`^${source}$`, 'i').test(id)
    })
}

function runDotnet(args, options = {}) {
    const result = spawnSync('dotnet', args, { cwd: repositoryRoot, stdio: 'inherit', ...options })
    if (result.error?.code === 'ENOENT') {
        throw new Error(
            'The .NET SDK (dotnet) was not found. Install the version from global.json.',
        )
    }
    if (result.error) {
        throw new Error(`Cannot run dotnet: ${result.error.message}`, { cause: result.error })
    }

    return result
}

/**
 * Returns the NuGet global packages folder, which also holds the restored local .NET tools.
 */
export function globalPackagesDirectory() {
    const result = runDotnet(['nuget', 'locals', 'global-packages', '--list'], {
        stdio: 'pipe',
        encoding: 'utf8',
    })
    const match = result.status === 0 && /global-packages:\s*(.+)/.exec(result.stdout)
    if (!match) {
        throw new Error('Cannot find the NuGet global packages folder')
    }

    return match[1].trim()
}

/**
 * Checks all NuGet packages (including transitive ones) of the solution with nuget-license.
 * Returns one result per package (name, version, license, approved, errors).
 */
export function checkNuGet(policy) {
    if (runDotnet(['tool', 'restore']).status !== 0) {
        throw new Error('dotnet tool restore failed')
    }
    if (runDotnet(['restore', policy.nuget.solution, '-v', 'q']).status !== 0) {
        throw new Error(`dotnet restore ${policy.nuget.solution} failed`)
    }

    const outputDirectory = mkdtempSync(path.join(os.tmpdir(), 'wavemp-nuget-license-'))
    try {
        const outputFile = path.join(outputDirectory, 'licenses.json')
        const args = [
            'tool',
            'run',
            'nuget-license',
            '--input',
            policy.nuget.solution,
            '--include-transitive',
            '--allowed-license-types',
            policy.allowedLicenses.join(';'),
            '--output',
            'Json',
            '--file-output',
            outputFile,
        ]
        if (policy.nuget.allowedPackages.length > 0) {
            args.push('--ignored-packages', policy.nuget.allowedPackages.join(';'))
        }

        const { status } = runDotnet(args)
        if (!existsSync(outputFile)) {
            throw new Error(`nuget-license failed with exit code ${status} and wrote no result`)
        }

        const results = JSON.parse(readFileSync(outputFile, 'utf8')).map((item) => {
            const errors = (item.ValidationErrors ?? []).map((error) => error.Error)
            return {
                name: item.PackageId,
                version: item.PackageVersion,
                license: item.License,
                approved: errors.length === 0,
                errors: [...new Set(errors)],
            }
        })
        if (status !== 0 && results.every((result) => result.approved)) {
            throw new Error(`nuget-license failed with exit code ${status}`)
        }

        return results
    } finally {
        rmSync(outputDirectory, { recursive: true, force: true })
    }
}

/**
 * Returns the path of the directory entry whose name equals the given name ignoring case, or
 * undefined if there is none. An entry with exactly that name is preferred.
 */
function findEntryIgnoringCase(directory, name) {
    const exact = path.join(directory, name)
    if (existsSync(exact)) {
        return exact
    }

    let entries
    try {
        entries = readdirSync(directory)
    } catch {
        return undefined
    }
    const lowerName = name.toLowerCase()
    const entry = entries.find((item) => item.toLowerCase() === lowerName)

    return entry === undefined ? undefined : path.join(directory, entry)
}

/**
 * Returns the path of the nuspec file of a restored package, or undefined if it is missing.
 * NuGet names the package and version folders in lowercase, but `dotnet tool restore` keeps the
 * original case of the package ID in the nuspec file name (e.g. Microsoft.OpenApi.Kiota.nuspec).
 * Linux file systems are case-sensitive, so every part of the path is matched ignoring case.
 */
function findPackageNuspec(packagesDirectory, id, version) {
    const lowerId = id.toLowerCase()
    let file = packagesDirectory
    for (const name of [lowerId, version.toLowerCase(), `${lowerId}.nuspec`]) {
        file = findEntryIgnoringCase(file, name)
        if (file === undefined) {
            return undefined
        }
    }

    return file
}

function readToolLicense(packagesDirectory, id, version) {
    const file = findPackageNuspec(packagesDirectory, id, version)
    if (file === undefined) {
        const lowerId = id.toLowerCase()
        const folder = path.join(packagesDirectory, lowerId, version.toLowerCase())
        throw new Error(
            `The .NET tool ${id} ${version} is not restored (no ${lowerId}.nuspec in ${folder})`,
        )
    }
    const match = /<license\b[^>]*\btype="expression"[^>]*>([^<]+)<\/license>/.exec(
        readFileSync(file, 'utf8'),
    )

    return match?.[1].trim()
}

/**
 * Checks the local .NET tools of the tool manifests by the license expression of their packages
 * (a tool package contains its dependencies). Expects restored tools in the packages directory.
 * Returns one result per tool (name, version, license, approved, errors).
 */
export function checkDotnetTools(policy, packagesDirectory) {
    const results = []
    for (const manifest of policy.nuget.toolManifests) {
        const manifestFile = path.resolve(repositoryRoot, manifest)
        const tools = JSON.parse(readFileSync(manifestFile, 'utf8')).tools ?? {}
        for (const [id, tool] of Object.entries(tools)) {
            const version = String(tool.version)
            const license = readToolLicense(packagesDirectory, id, version)
            if (matchesPackagePattern(id, policy.nuget.allowedPackages)) {
                results.push({ name: id, version, license, approved: true })
            } else if (license === undefined) {
                const errors = ['the package has no SPDX license expression']
                results.push({ name: id, version, license, approved: false, errors })
            } else {
                const approved = isLicenseAllowed(license, policy.allowedLicenses)
                results.push({ name: id, version, license, approved })
            }
        }
    }

    return results
}

function describeLicense(license) {
    if (typeof license === 'string' && license.length > 0) {
        return license
    }

    return license ? JSON.stringify(license) : 'no license metadata'
}

/**
 * Prints the packages that are not allowed and a summary. Returns the number of those packages.
 */
function report(title, results) {
    const rejected = results.filter((result) => !result.approved)
    for (const result of rejected) {
        const details = result.errors?.length > 0 ? ` (${result.errors.join('; ')})` : ''
        console.error(
            `  ${result.name}@${result.version}: ${describeLicense(result.license)}${details}`,
        )
    }
    console.log(`${title}: ${results.length} packages checked, ${rejected.length} not allowed`)

    return rejected.length
}

export async function main(argv) {
    try {
        const { values } = parseArgs({ args: argv, options: { policy: { type: 'string' } } })
        const policyFile = values.policy ? path.resolve(values.policy) : defaultPolicyFile
        const policy = loadPolicy(policyFile)

        const uncovered = findUncoveredProjects(
            policy,
            listProjectFiles(),
            readSolutionProjects(policy.nuget.solution),
        )
        for (const problem of uncovered) {
            console.error(`  ${problem}`)
        }
        console.log(`Projects: ${uncovered.length} not covered by the license check`)

        ensureNpmPackages(scriptsDirectory)
        let rejected = report(`NuGet ${policy.nuget.solution}`, checkNuGet(policy))
        rejected += report(
            `NuGet tools ${policy.nuget.toolManifests.join(', ') || '(no tool manifest)'}`,
            checkDotnetTools(policy, globalPackagesDirectory()),
        )
        for (const project of policy.npm.projects) {
            const directory = path.resolve(repositoryRoot, project)
            ensureNpmPackages(directory)
            rejected += report(`npm ${project}`, await checkNpmProject(directory, policy))
        }

        let failed = false
        if (uncovered.length > 0) {
            console.error(
                `${uncovered.length} projects are not covered: add .NET projects to the solution, ` +
                    `tool manifests and npm projects to ${relativeToRoot(policyFile)}.`,
            )
            failed = true
        }
        if (rejected > 0) {
            console.error(
                `${rejected} packages have a license that is not allowed by ${relativeToRoot(policyFile)}.`,
            )
            failed = true
        }
        if (failed) {
            return 1
        }

        console.log('All dependencies have an allowed license.')
        return 0
    } catch (error) {
        console.error(`License check failed: ${error.message}`)
        return 1
    }
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
    process.exitCode = await main(process.argv.slice(2))
}
