(() => {
    let active = null;
    let hideTimer;

    const close = () => {
        clearTimeout(hideTimer);
        if (active) active.tooltip.hidden = true;
        active = null;
    };

    const position = (entry) => {
        const anchor = entry.button.getBoundingClientRect();
        const box = entry.tooltip.getBoundingClientRect();
        const left = Math.max(12, Math.min(anchor.left, window.innerWidth - box.width - 12));
        const top = anchor.top >= box.height + 12 ? anchor.top - box.height - 8 : anchor.bottom + 8;
        entry.tooltip.style.left = `${left}px`;
        entry.tooltip.style.top = `${Math.max(12, Math.min(top, window.innerHeight - box.height - 12))}px`;
    };

    const open = (entry) => {
        clearTimeout(hideTimer);
        if (active !== entry) close();
        active = entry;
        // The body overlay stays outside the scrollable table and mobile cards.
        document.body.append(entry.tooltip);
        entry.tooltip.hidden = false;
        position(entry);
    };

    const scheduleClose = (entry) => {
        clearTimeout(hideTimer);
        hideTimer = setTimeout(() => {
            if (active === entry && document.activeElement !== entry.button
                && !entry.button.matches(':hover') && !entry.tooltip.matches(':hover')) close();
        }, 120);
    };

    document.querySelectorAll('[data-course-visibility]').forEach((wrapper) => {
        const entry = { button: wrapper.querySelector('button'), tooltip: wrapper.querySelector('[role="tooltip"]') };
        entry.button.addEventListener('mouseenter', () => open(entry));
        entry.button.addEventListener('mouseleave', () => scheduleClose(entry));
        entry.button.addEventListener('focus', () => open(entry));
        entry.button.addEventListener('blur', () => scheduleClose(entry));
        entry.button.addEventListener('click', () => open(entry));
        entry.tooltip.addEventListener('mouseenter', () => clearTimeout(hideTimer));
        entry.tooltip.addEventListener('mouseleave', () => scheduleClose(entry));
    });

    document.addEventListener('keydown', (event) => {
        if (event.key === 'Escape' && active) {
            close();
            event.preventDefault();
        }
    });
    document.addEventListener('click', (event) => {
        if (active && !active.button.contains(event.target) && !active.tooltip.contains(event.target)) close();
    });
    window.addEventListener('resize', close);
    document.addEventListener('scroll', () => {
        // Keep descriptions available during the browser's scroll to a keyboard focus target.
        if (active) position(active);
    }, true);
})();
