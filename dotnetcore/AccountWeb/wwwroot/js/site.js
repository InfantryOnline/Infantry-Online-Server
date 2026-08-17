(() => {
  const storageKey = "infantry-online-theme";
  const root = document.documentElement;
  const toggle = document.getElementById("themeToggle");

  const setTheme = (theme) => {
    root.setAttribute("data-bs-theme", theme);

    if (toggle) {
      toggle.textContent = theme === "dark" ? "Light" : "Dark";
      toggle.setAttribute("aria-label", theme === "dark" ? "Switch to light mode" : "Switch to dark mode");
    }
  };

  const savedTheme = localStorage.getItem(storageKey);
  setTheme(savedTheme === "light" ? "light" : "dark");

  if (toggle) {
    toggle.addEventListener("click", () => {
      const nextTheme = root.getAttribute("data-bs-theme") === "dark" ? "light" : "dark";
      localStorage.setItem(storageKey, nextTheme);
      setTheme(nextTheme);
    });
  }
})();
