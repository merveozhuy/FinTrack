import { useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import { PageHeader } from '../components/ui'

export function ProfilePage() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()

  function handleLogout() {
    logout()
    navigate('/login', { replace: true })
  }

  const initial = (user?.displayName || user?.email || '?').charAt(0).toUpperCase()

  return (
    <div>
      <PageHeader title="Profil" subtitle="Hesap bilgileriniz" />

      <div className="grid gap-4 lg:grid-cols-2">
        <div className="card">
          <div className="flex items-center gap-4">
            <span className="flex h-16 w-16 items-center justify-center rounded-2xl bg-brand-600 text-2xl font-bold text-white">
              {initial}
            </span>
            <div>
              <p className="text-lg font-semibold text-slate-900">{user?.displayName}</p>
              <p className="text-sm text-slate-500">{user?.email}</p>
            </div>
          </div>
          <dl className="mt-6 space-y-3 text-sm">
            <div className="flex justify-between border-t border-slate-100 pt-3">
              <dt className="text-slate-500">Ad</dt>
              <dd className="font-medium text-slate-800">{user?.displayName}</dd>
            </div>
            <div className="flex justify-between border-t border-slate-100 pt-3">
              <dt className="text-slate-500">E-posta</dt>
              <dd className="font-medium text-slate-800">{user?.email}</dd>
            </div>
          </dl>
          <button className="btn-danger mt-6 w-full" onClick={handleLogout}>Çıkış yap</button>
        </div>

        <div className="card">
          <h3 className="font-semibold text-slate-900">FinTrack hakkında</h3>
          <p className="mt-2 text-sm leading-relaxed text-slate-600">
            FinTrack; gelir, gider, bütçe ve yinelenen ödemelerinizi tek yerden yönetmenizi sağlayan
            kişisel bir finans uygulamasıdır. Finansal hesaplamalar güvenilir şekilde arka uçta yapılır;
            AI asistan yalnızca kendi verilerinizi açıklamak için kullanılır.
          </p>
          <dl className="mt-4 space-y-2 text-sm">
            <div className="flex justify-between">
              <dt className="text-slate-500">Para birimi</dt>
              <dd className="font-medium text-slate-800">₺ TRY</dd>
            </div>
            <div className="flex justify-between">
              <dt className="text-slate-500">Sürüm</dt>
              <dd className="font-medium text-slate-800">1.0</dd>
            </div>
          </dl>
        </div>
      </div>
    </div>
  )
}
