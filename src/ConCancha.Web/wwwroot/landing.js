window.concancha = window.concancha || {};

window.concancha.revealOnView = function (elementId) {
    const element = document.getElementById(elementId);
    if (!element || !('IntersectionObserver' in window)) return;

    element.classList.add('is-armed');
    const observer = new IntersectionObserver(entries => {
        if (!entries.some(entry => entry.isIntersecting)) return;
        element.classList.add('is-visible');
        observer.disconnect();
    }, { threshold: 0.5 });
    observer.observe(element);
};
