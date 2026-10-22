window.concancha = window.concancha || {};

window.concancha.scrollToNow = function (gridId) {
    const grid = document.getElementById(gridId);
    const line = grid && grid.querySelector('.nowline');
    if (!line) return;

    const offsetFromTop = line.getBoundingClientRect().top - grid.getBoundingClientRect().top;
    if (grid.scrollHeight > grid.clientHeight) {
        grid.scrollTop += offsetFromTop - grid.clientHeight / 3;
        return;
    }
    window.scrollBy(0, line.getBoundingClientRect().top - window.innerHeight / 3);
};
