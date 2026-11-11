// License check for all NuGet and npm dependencies of WaveMP, used locally and in CI.
//
// Usage (from any directory): node .github/scripts/check-licenses.mjs [--policy <file>]
//
// The allowed licenses (SPDX identifiers) and the checked projects are configured in
// .github/license-policy.json. NuGet packages are checked with nuget-license (local .NET tool),
// npm packages with licensee. Missing tools and packages are restored automatically.
// Exceptions for single packages are only added to the policy after a manual license review.
// Exits with 0 if every dependency has an allowed license, otherwise with 1.

import { spawnSync } from 'node:child_process'
import { existsSync, mkdtempSync, readFileSync, rmSync, statSync } from 'node:fs'
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
    if (typeof policy?.nuget?.solution !== 'string') {
        problems.push('"nuget.solution" must be the path of the solution file')
    }
    if (!isStringArray(policy?.nuget?.allowedPackages)) {
        problems.push('"nuget.allowedPackages" must be an array of package IDs')
    }
    if (!isStringArray(policy?.npm?.projects)) {
        problems.push('"npm.projects" must be an array of project directories')
    }
    if (!isStringMap(policy?.npm?.allowedPackages)) {
        problems.push('"npm.allowedPackages" must map package names to semver ranges')
    }
    if (problems.length > 0) {
        throw new Error(`Invalid license policy ${file}: ${problems.join('; ')}`)
    }

    return policy
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
    const licensee = createRequire(path.join(scriptsDirectory, 'package.json'))('licensee')
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

function runDotnet(args) {
    const result = spawnSync('dotnet', args, { cwd: repositoryRoot, stdio: 'inherit' })
    if (result.error?.code === 'ENOENT') {
        throw new Error(
            'The .NET SDK (dotnet) was not found. Install the version from global.json.',
        )
    }
    if (result.error) {
        throw new Error(`Cannot run dotnet: ${result.error.message}`, { cause: result.error })
    }

    return result.status
}

/**
 * Checks all NuGet packages (including transitive ones) of the solution with nuget-license.
 * Returns one result per package (name, version, license, approved, errors).
 */
export function checkNuGet(policy) {
    if (runDotnet(['tool', 'restore']) !== 0) {
        throw new Error('dotnet tool restore failed')
    }
    if (runDotnet(['restore', policy.nuget.solution, '-v', 'q']) !== 0) {
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

        const status = runDotnet(args)
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
        ensureNpmPackages(scriptsDirectory)

        let rejected = report(`NuGet ${policy.nuget.solution}`, checkNuGet(policy))
        for (const project of policy.npm.projects) {
            const directory = path.resolve(repositoryRoot, project)
            ensureNpmPackages(directory)
            rejected += report(`npm ${project}`, await checkNpmProject(directory, policy))
        }

        if (rejected > 0) {
            console.error(
                `${rejected} packages have a license that is not allowed by ${relativeToRoot(policyFile)}.`,
            )
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
