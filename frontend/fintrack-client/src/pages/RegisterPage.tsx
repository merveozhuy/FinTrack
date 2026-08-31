import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Link, Navigate, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import { useToast } from '../context/ToastContext'
import { getApiErrorMessage } from '../lib/api'
import { Field } from '../components/ui'

const schema = z.object({
  displayName: z.string().min(1, 'Ad gerekli.').max(128),
  email: z.string().email('Geçerli bir e-posta girin.'),
  password: z.string().min(8, 'Şifre en az 8 karakter olmalı.'),
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
      notify('success', 'Hesap oluşturuldu. Hoş geldiniz!')
      navigate('/', { replace: true })
    } catch (error) {
      notify('error', getApiErrorMessage(error, 'Kayıt başarısız.'))
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-900 p-4">
      <div className="w-full max-w-sm">
        <div className="mb-6 text-center text-white">
          <div className="mx-auto mb-3 flex h-12 w-12 items-center justify-center rounded-2xl bg-brand-500 text-xl font-bold">₺</div>
          <h1 className="text-2xl font-semibold">Hesap oluşturun</h1>
          <p className="mt-1 text-sm text-slate-400">Finanslarınızı takip etmeye başlayın</p>
        </div>
        <form onSubmit={handleSubmit(onSubmit)} className="card space-y-4">
          <Field label="Ad" error={formState.errors.displayName?.message}>
            <input className="input" autoComplete="name" {...register('displayName')} />
          </Field>
          <Field label="E-posta" error={formState.errors.email?.message}>
            <input className="input" type="email" autoComplete="email" {...register('email')} />
          </Field>
          <Field label="Şifre" error={formState.errors.password?.message}>
            <input className="input" type="password" autoComplete="new-password" {...register('password')} />
          </Field>
          <button type="submit" className="btn-primary w-full" disabled={formState.isSubmitting}>
            {formState.isSubmitting ? 'Oluşturuluyor…' : 'Hesap oluştur'}
          </button>
        </form>
        <p className="mt-4 text-center text-sm text-slate-400">
          Zaten hesabınız var mı?{' '}
          <Link to="/login" className="font-medium text-brand-500 hover:underline">Giriş yapın</Link>
        </p>
      </div>
    </div>
  )
}
