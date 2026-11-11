// Guard for the rules of CLAUDE.md for components: no fixed texts (everything goes through
// vue-i18n) and no styles (everything visual belongs to the base CSS in src/styles and to the
// themes, which address the DOM through data-part attributes).
import { readdirSync, readFileSync, statSync } from 'node:fs'
import path from 'node:path'
import { fileURLToPath } from 'node:url'
import { describe, expect, it } from 'vitest'
import { parse } from 'vue/compiler-sfc'

// Not new URL(..., import.meta.url): Vite rewrites that pattern into an asset URL.
const sourceDirectory = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..')

// Node types of the template AST (@vue/compiler-core NodeTypes).
const elementNode = 1
const textNode = 2
const attributeNode = 6
const directiveNode = 7

// Attributes that contain text for people and therefore must come from the messages.
const textAttributes = new Set([
    'title',
    'placeholder',
    'aria-label',
    'aria-description',
    'aria-valuetext',
    'alt',
    'label',
])

interface TemplateNode {
    type: number
    tag?: string
    content?: string
    children?: TemplateNode[]
    props?: TemplateProp[]
    loc: { start: { line: number } }
}

interface TemplateProp {
    type: number
    name: string
    value?: { content: string }
    arg?: { content: string }
    exp?: { content: string }
}

function vueFiles(directory: string): string[] {
    return readdirSync(directory).flatMap((name) => {
        const file = path.join(directory, name)
        if (statSync(file).isDirectory()) {
            return name === '__tests__' ? [] : vueFiles(file)
        }
        return name.endsWith('.vue') ? [file] : []
    })
}

/** True if a :style expression only sets runtime variables of the theme API (--wmp-*). */
export function isAllowedStyleBinding(expression: string): boolean {
    const trimmed = expression.trim()
    if (!trimmed.startsWith('{') || !trimmed.endsWith('}')) {
        return false
    }
    if (/[{,]\s*[A-Za-z_$][\w$]*\s*:/.test(trimmed)) {
        return false
    }
    const keys = [...trimmed.matchAll(/'([^']*)'\s*:/g)].map((match) => match[1] ?? '')
    return keys.length > 0 && keys.every((key) => key.startsWith('--wmp-'))
}

function check(file: string): string[] {
    const source = readFileSync(file, 'utf8')
    const { descriptor, errors } = parse(source, { filename: file })
    const problems: string[] = errors.map((error) => `parse error: ${String(error)}`)

    if (descriptor.styles.length > 0) {
        problems.push('has a <style> block')
    }

    function visit(node: TemplateNode): void {
        const line = node.loc.start.line
        if (node.type === textNode && (node.content ?? '').trim() !== '') {
            problems.push(`line ${line}: fixed text "${(node.content ?? '').trim()}"`)
        }

        if (node.type === elementNode) {
            for (const prop of node.props ?? []) {
                if (prop.type === attributeNode) {
                    const value = prop.value?.content.trim() ?? ''
                    if (textAttributes.has(prop.name) && value !== '') {
                        problems.push(`line ${line}: fixed text in ${prop.name}="${value}"`)
                    }
                    if (prop.name === 'class') {
                        problems.push(`line ${line}: class attribute`)
                    }
                    if (prop.name === 'style') {
                        problems.push(`line ${line}: style attribute`)
                    }
                }

                if (prop.type === directiveNode && prop.name === 'bind') {
                    const argument = prop.arg?.content
                    if (argument === 'class') {
                        problems.push(`line ${line}: :class binding`)
                    }
                    if (argument === 'style' && !isAllowedStyleBinding(prop.exp?.content ?? '')) {
                        problems.push(`line ${line}: :style may only set --wmp-* variables`)
                    }
                }
            }
        }

        for (const child of node.children ?? []) {
            visit(child)
        }
    }

    const ast = descriptor.template?.ast as TemplateNode | undefined
    if (ast) {
        visit(ast)
    }

    return problems
}

describe('component rules', () => {
    const files = vueFiles(sourceDirectory)

    it('finds the components', () => {
        expect(files.length).toBeGreaterThan(0)
    })

    for (const file of files) {
        it(`${path.relative(sourceDirectory, file).replaceAll('\\', '/')} has no fixed texts and no styles`, () => {
            expect(check(file)).toEqual([])
        })
    }
})

describe('isAllowedStyleBinding', () => {
    it('allows only theme API runtime variables', () => {
        expect(isAllowedStyleBinding("{ '--wmp-slider-value': ratio }")).toBe(true)
        expect(isAllowedStyleBinding("{ '--wmp-menu-x': `${x}px`, '--wmp-menu-y': y }")).toBe(true)
        expect(isAllowedStyleBinding("{ color: 'red' }")).toBe(false)
        expect(isAllowedStyleBinding("{ 'color': 'red' }")).toBe(false)
        expect(isAllowedStyleBinding('styles')).toBe(false)
    })
})
