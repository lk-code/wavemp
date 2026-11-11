// Breakpoint of the theme API: below it, the fixed mobile base applies (src/styles/mobile.css);
// from it on, the layout of the theme. Media queries cannot use variables, so the value is fixed
// and part of the theme API. A unit test checks that the CSS uses exactly these conditions.

/** Desktop width: the layout of the theme applies. */
export const desktopMediaQuery = '(width >= 64rem)'

/** Mobile width: the fixed mobile base applies. */
export const mobileMediaQuery = '(width < 64rem)'
