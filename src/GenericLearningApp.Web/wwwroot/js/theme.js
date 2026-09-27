// Theme is a per-device preference, so it stays in localStorage rather than the database.
(function () {
  const KEY = "gla.theme";

  function apply(theme) {
    if (theme) document.documentElement.setAttribute("data-theme", theme);
    else document.documentElement.removeAttribute("data-theme");
  }

  try { apply(localStorage.getItem(KEY)); } catch { /* private mode: fall back to the OS theme */ }

  window.glaToggleTheme = function () {
    const prefersDark = window.matchMedia("(prefers-color-scheme: dark)").matches;
    const current = document.documentElement.getAttribute("data-theme") || (prefersDark ? "dark" : "light");
    const next = current === "dark" ? "light" : "dark";
    apply(next);
    try { localStorage.setItem(KEY, next); } catch { /* nothing to do */ }
    return next;
  };
})();
