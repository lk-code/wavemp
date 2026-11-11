<script setup lang="ts">
import { computed, ref, useId, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import type { UserListItem } from './UserList.vue'
import {
    ProblemCodeObject,
    RoleObject,
    type CreateUser,
    type Role,
    type UpdateUser,
    type User,
} from '@/api/generated/models'
import { problemCodeOf, statusOf, validationErrorsOf } from '@/api/problems'
import BaseAlert from '@/components/base/BaseAlert.vue'
import BaseButton from '@/components/base/BaseButton.vue'
import BaseColorPicker, { type ColorOption } from '@/components/base/BaseColorPicker.vue'
import BaseDialog from '@/components/base/BaseDialog.vue'
import BaseSelect, { type SelectOption } from '@/components/base/BaseSelect.vue'
import BaseTextField from '@/components/base/BaseTextField.vue'
import { validationMessagesByField } from '@/i18n/validation'
import { profileColorOptions } from '@/profiles/presentation'
import { useUsersStore } from '@/stores/users'

/**
 * Creates a profile (without user) or edits one. Only changed values are sent; empty initials
 * are derived from the name by the server.
 */
const props = defineProps<{
    user: UserListItem | null
}>()

const emit = defineEmits<{
    saved: [user: User]
}>()

const open = defineModel<boolean>('open', { default: false })

const { t } = useI18n()
const users = useUsersStore()
const formId = useId()

const name = ref('')
const initials = ref('')
const color = ref<string | null>(null)
const role = ref<Role>(RoleObject.User)
const nameError = ref<string | undefined>(undefined)
const initialsError = ref<string | undefined>(undefined)
const error = ref<string | null>(null)
const saving = ref(false)

const editing = computed(() => props.user !== null)
const builtIn = computed(() => !!props.user?.isBuiltIn)

const colorOptions = computed<ColorOption[]>(() => {
    const palette = profileColorOptions.map((option) => ({
        value: option.value,
        label: t(option.labelKey),
    }))
    const current = props.user?.color
    // A color set outside of the palette (e.g. through the API) stays selectable.
    return current && !palette.some((option) => option.value === current.toLowerCase())
        ? [...palette, { value: current, label: t('adminUsers.dialog.currentColor') }]
        : palette
})

const roleOptions = computed<SelectOption[]>(() => [
    { value: RoleObject.User, label: t('adminUsers.roles.user') },
    { value: RoleObject.Admin, label: t('adminUsers.roles.admin') },
])

function reset(): void {
    name.value = props.user?.name ?? ''
    initials.value = props.user?.initials ?? ''
    color.value = props.user?.color ?? null
    role.value = props.user?.isAdmin ? RoleObject.Admin : RoleObject.User
    nameError.value = undefined
    initialsError.value = undefined
    error.value = null
}

watch(open, (value) => {
    if (value) {
        reset()
    }
})

/** The changed values of an existing profile. */
function changes(user: UserListItem): UpdateUser {
    const update: UpdateUser = {}
    const trimmedName = name.value.trim()
    if (trimmedName !== user.name) {
        update.name = trimmedName
    }
    if (initials.value.trim() !== user.initials) {
        update.initials = initials.value.trim()
    }
    if (color.value && color.value !== user.color) {
        update.color = color.value
    }
    const userRole = user.isAdmin ? RoleObject.Admin : RoleObject.User
    if (role.value !== userRole && !user.isBuiltIn) {
        update.role = role.value
    }
    return update
}

function newUser(): CreateUser {
    return {
        name: name.value.trim(),
        initials: initials.value.trim() || undefined,
        color: color.value ?? undefined,
        role: role.value,
    }
}

function report(failure: unknown): void {
    const code = problemCodeOf(failure)
    if (code === ProblemCodeObject.UserName_taken) {
        nameError.value = t('adminUsers.errors.nameTaken')
    } else if (code === ProblemCodeObject.UserNot_found) {
        error.value = t('adminUsers.errors.notFound')
    } else if (statusOf(failure) === 400) {
        const messages = validationMessagesByField(
            validationErrorsOf(failure),
            { name: 'name', initials: 'initials' } as const,
            (key, parameters) => t(key, parameters),
        )
        nameError.value = messages.name
        initialsError.value = messages.initials
        error.value = t('adminUsers.errors.invalid')
    } else {
        error.value = t('adminUsers.errors.saveFailed')
    }
}

async function save(): Promise<void> {
    if (saving.value) {
        return
    }

    nameError.value = undefined
    initialsError.value = undefined
    error.value = null
    if (name.value.trim() === '') {
        nameError.value = t('adminUsers.errors.nameRequired')
        return
    }

    saving.value = true
    try {
        let saved: User | undefined
        if (props.user) {
            const update = changes(props.user)
            if (Object.keys(update).length === 0) {
                open.value = false
                return
            }
            saved = await users.update(props.user.id, update)
        } else {
            saved = await users.create(newUser())
        }

        open.value = false
        if (saved) {
            emit('saved', saved)
        }
    } catch (failure) {
        report(failure)
    } finally {
        saving.value = false
    }
}
</script>

<template>
    <BaseDialog
        v-model:open="open"
        :title="editing ? t('adminUsers.dialog.editTitle') : t('adminUsers.dialog.createTitle')"
    >
        <form :id="formId" data-part="form" novalidate @submit.prevent="save">
            <BaseAlert v-if="error" tone="error" :message="error" />
            <BaseTextField
                v-model="name"
                :label="t('adminUsers.dialog.name')"
                :error="nameError"
                :maxlength="50"
                autocomplete="off"
                required
            />
            <BaseTextField
                v-model="initials"
                :label="t('adminUsers.dialog.initials')"
                :hint="t('adminUsers.dialog.initialsHint')"
                :error="initialsError"
                :maxlength="16"
                autocomplete="off"
            />
            <BaseColorPicker
                v-model="color"
                :label="t('adminUsers.dialog.color')"
                :options="colorOptions"
            />
            <BaseSelect
                v-model="role"
                :label="t('adminUsers.dialog.role')"
                :options="roleOptions"
                :disabled="builtIn"
                :hint="builtIn ? t('adminUsers.dialog.roleBuiltInHint') : undefined"
            />
        </form>
        <template #footer>
            <BaseButton :label="t('actions.cancel')" @click="open = false" />
            <BaseButton
                type="submit"
                variant="primary"
                :form="formId"
                :label="t('adminUsers.dialog.save')"
                :disabled="saving"
            />
        </template>
    </BaseDialog>
</template>
