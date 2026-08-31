import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Link, Navigate, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import { useToast } from '../context/ToastContext'
import { getApiErrorMessage } from '../lib/api'
import { Field } from '../components/ui'

const schema = z.object({
  displayName: z.string().min(1, 'Name is required.').max(128),
  email: z.string().email('Enter a valid email address.'),
  password: z.string().min(8, 'Password must be at least 8 characters.'),
})

type FormValues = z.infer<typeof schema>

export function RegisterPage() {
  const { register: registerUser, isAuthenticated } = useAuth()
  const { notify } = useToast()
  const navigate = useNavigate()
  const { register, handleSubmit, formState } = useForm<FormValues>({ resolver: zodResolver(schema) })

  if (isAuthenticated) return <Navigate to="/" replace />

  async function onSubmit(values: FormValues) {
    try {
      await registerUser(values.email, values.password, values.displayName)
      notify('success', 'Account created. Welcome!')
      navigate('/', { replace: true })
    } catch (error) {
      notify('error', getApiErrorMessage(error, 'Registration failed.'))
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center p-4">
      <div className="w-full max-w-sm">
        <div className="mb-6 text-center">
          <div className="mx-auto mb-3 flex h-12 w-12 items-center justify-center rounded-xl bg-brand-600 text-xl font-bold text-white">₺</div>
          <h1 className="text-2xl font-semibold text-slate-900">Create your account</h1>
          <p className="mt-1 text-sm text-slate-500">Start tracking your finances</p>
        </div>
        <form onSubmit={handleSubmit(onSubmit)} className="card space-y-4">
          <Field label="Name" error={formState.errors.displayName?.message}>
            <input className="input" autoComplete="name" {...register('displayName')} />
          </Field>
          <Field label="Email" error={formState.errors.email?.message}>
            <input className="input" type="email" autoComplete="email" {...register('email')} />
          </Field>
          <Field label="Password" error={formState.errors.password?.message}>
            <input className="input" type="password" autoComplete="new-password" {...register('password')} />
          </Field>
          <button type="submit" className="btn-primary w-full" disabled={formState.isSubmitting}>
            {formState.isSubmitting ? 'Creating…' : 'Create account'}
          </button>
        </form>
        <p className="mt-4 text-center text-sm text-slate-500">
          Already have an account?{' '}
          <Link to="/login" className="font-medium text-brand-600 hover:underline">Sign in</Link>
        </p>
      </div>
    </div>
  )
}
