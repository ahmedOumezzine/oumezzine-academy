document.querySelector('.admin-password-toggle')?.addEventListener('click', function () {
    const input = document.getElementById('Password');
    if (!input) return;
    const visible = input.type === 'text';
    input.type = visible ? 'password' : 'text';
    this.setAttribute('aria-pressed', String(!visible));
    this.setAttribute('aria-label', visible ? 'Afficher le mot de passe' : 'Masquer le mot de passe');
    const label = this.querySelector('span');
    const icon = this.querySelector('i');
    if (label) label.textContent = visible ? 'Afficher' : 'Masquer';
    if (icon) icon.className = visible ? 'bi bi-eye' : 'bi bi-eye-slash';
});
