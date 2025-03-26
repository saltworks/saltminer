export default async function ({ store, redirect }) {
    await store.dispatch('modules/user/fetchUser', { root: true })
    const config = await fetch('/smpgui/config.json').then((res) => res.json())
    if (
      !store.state.modules.user.roles.includes(config.view_only_role) &&
      !store.state.modules.user.roles.includes(config.admin_role) &&
      !store.state.modules.user.roles.includes(config.user_role) &&
      !store.state.modules.user.roles.includes(config.pentest_admin_role)) {
        return redirect(config.redirect_url)
    }
  }