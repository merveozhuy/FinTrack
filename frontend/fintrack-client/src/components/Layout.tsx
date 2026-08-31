import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'

const navItems = [
  { to: '/', label: 'Panel', icon: 'M4 4h7v7H4zM13 4h7v7h-7zM13 13h7v7h-7zM4 13h7v7H4z', end: true },
  { to: '/transactions', label: 'İşlemler', icon: 'M4 6h16M4 12h16M4 18h10' },
  { to: '/categories', label: 'Kategoriler', icon: 'M9 5H5a2 2 0 00-2 2v4l9 9 6-6-9-9zM8 8h.01' },
  { to: '/budgets', label: 'Bütçeler', icon: 'M12 3v9l6 4M12 21a9 9 0 110-18' },
  { to: '/recurring', label: 'Yinelenen', icon: 'M4 9a8 8 0 0113-4l3 3M20 15a8 8 0 01-13 4l-3-3M17 4v4h-4M7 20v-4h4' },
  { to: '/assistant', label: 'AI Asistan', icon: 'M8 10h8M8 14h5M21 12a8 8 0 01-8 8H7l-4 3V12a8 8 0 018-8h2a8 8 0 018 8z' },
  { to: '/profile', label: 'Profil', icon: 'M20 21a8 8 0 10-16 0M12 11a4 4 0 100-8 4 4 0 000 8z' },
]

function Icon({ path }: { path: string }) {
  return (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={1.8} strokeLinecap="round" strokeLinejoin="round" className="h-5 w-5">
      <path d={path} />
    </svg>
  )
}

export function Layout() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()

  function handleLogout() {
    logout()
    navigate('/login', { replace: true })
  }

  return (
    <div className="min-h-screen md:flex">
      <aside className="bg-slate-900 text-slate-300 md:min-h-screen md:w-64">
        <div className="flex items-center gap-2.5 px-5 py-4">
          <span className="flex h-9 w-9 items-center justify-center rounded-xl bg-brand-500 font-bold text-white">₺</span>
          <div>
            <p className="text-base font-semibold text-white">FinTrack</p>
            <p className="text-[11px] text-slate-400">Kişisel Finans</p>
          </div>
        </div>
        <nav className="flex gap-1 overflow-x-auto px-3 pb-3 md:flex-col md:overflow-visible md:pb-4">
          {navItems.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.end}
              className={({ isActive }) =>
                `flex items-center gap-3 whitespace-nowrap rounded-xl px-3 py-2.5 text-sm font-medium transition-colors ${
                  isActive ? 'bg-slate-800 text-white' : 'text-slate-400 hover:bg-slate-800/60 hover:text-slate-200'
                }`
              }
            >
              <Icon path={item.icon} />
              {item.label}
            </NavLink>
          ))}
        </nav>
      </aside>

      <div className="flex-1">
        <header className="flex items-center justify-between border-b border-slate-200 bg-white px-6 py-3.5">
          <div className="text-sm text-slate-500">
            Merhaba, <span className="font-semibold text-slate-800">{user?.displayName ?? user?.email}</span> 👋
          </div>
          <button onClick={handleLogout} className="btn-secondary">Çıkış</button>
        </header>
        <main className="mx-auto max-w-6xl p-6">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
