// How profiles are presented: the built-in admin profile steps back (state "secondary") as soon as
// there are other profiles, so the profiles of the people come first.

export interface ProfileLike {
    readonly id?: string | null
    readonly isBuiltIn?: boolean | null
}

/** True for the built-in admin profile if there is at least one other profile. */
export function isSecondaryProfile(profile: ProfileLike, all: readonly ProfileLike[]): boolean {
    return !!profile.isBuiltIn && all.some((other) => other.id !== profile.id)
}

/** data-state tokens of a profile tile: built-in, secondary and current. */
export function profileTileStates(
    profile: ProfileLike,
    all: readonly ProfileLike[],
    currentId?: string | null,
): string | undefined {
    const tokens = [
        profile.isBuiltIn ? 'built-in' : undefined,
        isSecondaryProfile(profile, all) ? 'secondary' : undefined,
        currentId && profile.id === currentId ? 'current' : undefined,
    ].filter(Boolean)

    return tokens.length > 0 ? tokens.join(' ') : undefined
}

/** True on the first start: the built-in admin profile is the only one. */
export function isFirstStart(all: readonly ProfileLike[]): boolean {
    return all.length === 1 && !!all[0]?.isBuiltIn
}
