import type { MessageSchema } from './index'

// Type-checks the message keys used with t() against the English messages.
declare module 'vue-i18n' {
    // eslint-disable-next-line @typescript-eslint/no-empty-object-type
    export interface DefineLocaleMessage extends MessageSchema {}
}
