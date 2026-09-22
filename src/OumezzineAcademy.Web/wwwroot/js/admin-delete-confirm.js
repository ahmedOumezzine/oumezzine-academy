(() => {
    const dialog = document.querySelector("[data-admin-delete-dialog]");
    if (!dialog) return;

    const message = dialog.querySelector("#admin-delete-message");
    const cancelButton = dialog.querySelector("[data-delete-cancel]");
    const confirmButton = dialog.querySelector("[data-delete-confirm]");
    const approvedForms = new WeakSet();
    const approvedControls = new WeakSet();
    let pendingForm = null;
    let pendingSubmitter = null;
    let pendingControl = null;
    let returnFocus = null;

    document.addEventListener("submit", event => {
        const form = event.target;
        const submitter = event.submitter;
        if (!(form instanceof HTMLFormElement) || !submitter?.hasAttribute("data-confirm-delete")) return;

        if (approvedForms.has(form)) {
            approvedForms.delete(form);
            form.dataset.confirmSubmitted = "true";
            window.setTimeout(() => submitter.disabled = true, 0);
            return;
        }

        event.preventDefault();
        if (form.dataset.confirmSubmitted === "true" || dialog.open) return;

        pendingForm = form;
        pendingSubmitter = submitter;
        pendingControl = null;
        showConfirmation(submitter);
    });

    document.addEventListener("click", event => {
        const control = event.target.closest("[data-confirm-delete-action]");
        if (!control) return;
        if (approvedControls.has(control)) {
            approvedControls.delete(control);
            return;
        }
        event.preventDefault();
        if (dialog.open) return;
        pendingForm = null;
        pendingSubmitter = null;
        pendingControl = control;
        showConfirmation(control);
    }, true);

    function showConfirmation(trigger) {
        returnFocus = trigger;
        const item = trigger.dataset.confirmItem?.trim();
        const action = trigger.dataset.confirmMessage || "effectuer cette action";
        message.textContent = item
            ? `Êtes-vous sûr de vouloir ${action} « ${item} » ?`
            : `Êtes-vous sûr de vouloir ${action} ?`;
        dialog.showModal();
        cancelButton.focus();
    }

    dialog.addEventListener("close", () => {
        pendingForm = null;
        pendingSubmitter = null;
        pendingControl = null;
        returnFocus?.focus();
        returnFocus = null;
    });

    confirmButton.addEventListener("click", () => {
        if (pendingForm && pendingSubmitter) {
            const form = pendingForm;
            const submitter = pendingSubmitter;
            approvedForms.add(form);
            dialog.close();
            form.requestSubmit(submitter);
            return;
        }
        if (pendingControl) {
            const control = pendingControl;
            approvedControls.add(control);
            dialog.close();
            control.click();
        }
    });
})();