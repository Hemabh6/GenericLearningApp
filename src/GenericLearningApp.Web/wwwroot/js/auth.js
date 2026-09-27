// Sign-in pages are static server-rendered (no circuit), so their small behaviours are plain
// delegated DOM events. Delegation also survives Blazor's enhanced-navigation DOM patching.
(function () {
  document.addEventListener("click", function (e) {
    var toggle = e.target.closest("[data-theme-toggle]");
    if (toggle && window.glaToggleTheme) { window.glaToggleTheme(); return; }

    var show = e.target.closest("[data-show]");
    if (show) {
      var input = document.getElementById(show.dataset.show);
      if (!input) return;
      var hidden = input.type === "password";
      input.type = hidden ? "text" : "password";
      show.textContent = hidden ? "Hide" : "Show";
      show.setAttribute("aria-label", hidden ? "Hide password" : "Show password");
    }
  });

  // Same four rules the server enforces (length, lower + upper case, digit, symbol),
  // so a full meter means the password will be accepted.
  document.addEventListener("input", function (e) {
    var input = e.target;
    if (!input.matches || !input.matches("[data-strength]")) return;

    var field = input.closest(".sp-field");
    var hint = field && field.querySelector(".sp-hint");
    var bars = field ? field.querySelectorAll(".sp-meter i") : [];
    var min = parseInt(input.dataset.strength, 10) || 6;
    var v = input.value;

    var score = 0;
    if (v.length >= min) score++;
    if (/[a-z]/.test(v) && /[A-Z]/.test(v)) score++;
    if (/\d/.test(v)) score++;
    if (/[^A-Za-z0-9]/.test(v)) score++;

    bars.forEach(function (bar, i) { bar.className = i < score ? (score === 4 ? "ok" : "on") : ""; });

    if (hint) {
      if (!hint.dataset.default) hint.dataset.default = hint.textContent;
      var words = ["", "Weak", "Fair", "Good", "Strong"];
      hint.textContent = v
        ? "Strength: " + words[score] + (score < 4 ? ". Add length, capitals, numbers or a symbol." : ". Nice.")
        : hint.dataset.default;
    }
  });
})();
