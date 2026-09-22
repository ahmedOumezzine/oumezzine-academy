(() => {
    document.querySelectorAll('[data-confirm-delete-course]').forEach(form => {
        form.addEventListener('submit', event => {
            // Match the Admin's existing media/path confirmation, without inline script.
            if (!window.confirm(`Supprimer le cours « ${form.dataset.confirmDeleteCourse} » ?`)) {
                event.preventDefault();
            }
        });
    });
})();