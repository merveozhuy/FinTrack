import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Link, Navigate, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import { useToast } from '../context/ToastContext'
import { getApiErrorMessage } from '../lib/api'
import { Field } from '../components/ui'

const schema = z.object({
  email: z.string().email('Enter a valid email address.'),
  password: z.string().min(1, 'Password is required.'),
})

type FormValues = z.infer<typeof schema>

export function LoginPage() {
  const { login, isAuthenticated } = useAuth()
  const { notify } = useToast()
  const navigate = useNavigate()
  const { register, handleSubmit, formState } = useForm<FormValues>({ resolver: zodResolver(schema) })

  if (isAuthenticated) return <Navigate to="/" replace />

  async function onSubmit(values: FormValues) {
    try {
      await login(values.email, values.password)
      navigate('/', { replace: true })
    } catch (error) {
      notify('error', getApiErrorMessage(error, 'Login failed.'))
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center p-4">
      <div className="w-full max-w-sm">
        <div className="mb-6 text-center">
          <div className="mx-auto mb-3 flex h-12 w-12 items-center justify-center rounded-xl bg-brand-600 text-xl font-bold text-white">₺</div>
          <h1 className="text-2xl font-semibold text-slate-900">Welcome back</h1>
          <p className="mt-1 text-sm text-slate-500">Sign in to your FinTrack account</p>
        </div>
        <form onSubmit={handleSubmit(onSubmit)} className="card space-y-4">
          <Field label="Email" error={formState.errors.email?.message}>
            <input className="input" type="email" autoComplete="email" {...register('email')} />
          </Field>
          <Field label="Password" error={formState.errors.password?.message}>
            <input className="input" type="password" autoComplete="current-password" {...register('password')} />
          </Field>
          <button type="submit" className="btn-primary w-full" disabled={formState.isSubmitting}>
            {formState.isSubmitting ? 'Signing in…' : 'Sign in'}
          </button>
        </form>
        <p className="mt-4 text-center text-sm text-slate-500">
          No account?{' '}
          <Link to="/register" className="font-medium text-brand-600 hover:underline">Create one</Link>
        </p>
      </div>
    </div>
  )
}
