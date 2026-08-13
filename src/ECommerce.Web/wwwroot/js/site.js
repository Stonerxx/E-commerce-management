(function () {
    const storageKey = "ecommerce-theme";
    const mediaQuery = window.matchMedia("(prefers-color-scheme: dark)");

    function currentTheme() {
        return document.documentElement.getAttribute("data-theme") === "dark" ? "dark" : "light";
    }

    function updateThemeControls(theme) {
        document.querySelectorAll("[data-theme-toggle]").forEach(button => {
            const nextName = theme === "dark" ? "浅色" : "深色";
            button.setAttribute("aria-label", `切换为${nextName}模式`);
            button.setAttribute("title", `切换为${nextName}模式`);

            const icon = button.querySelector("[data-theme-icon]");
            if (!icon) return;
            if (icon.classList.contains("fa-solid")) {
                icon.className = theme === "dark" ? "fa-solid fa-sun" : "fa-solid fa-moon";
                icon.setAttribute("data-theme-icon", "");
            } else {
                icon.className = theme === "dark" ? "bi bi-sun" : "bi bi-moon-stars";
                icon.setAttribute("data-theme-icon", "");
            }
        });
    }

    function applyTheme(theme, persist) {
        const normalized = theme === "dark" ? "dark" : "light";
        document.documentElement.setAttribute("data-theme", normalized);
        if (persist) {
            try { localStorage.setItem(storageKey, normalized); } catch (_) { }
        }
        updateThemeControls(normalized);
        window.dispatchEvent(new CustomEvent("app:theme-changed", { detail: { theme: normalized } }));
    }

    function toast(message, type = "info") {
        const container = document.getElementById("appToastContainer");
        if (!container || !message) return;

        const tone = {
            success: "text-bg-success",
            danger: "text-bg-danger",
            warning: "text-bg-warning",
            info: "text-bg-primary"
        }[type] || "text-bg-primary";
        const icon = {
            success: "bi-check-circle-fill",
            danger: "bi-x-circle-fill",
            warning: "bi-exclamation-triangle-fill",
            info: "bi-info-circle-fill"
        }[type] || "bi-info-circle-fill";

        const element = document.createElement("div");
        element.className = `toast align-items-center border-0 ${tone}`;
        element.setAttribute("role", "status");
        element.setAttribute("aria-live", "polite");
        element.setAttribute("aria-atomic", "true");
        element.innerHTML = `
            <div class="d-flex">
                <div class="toast-body d-flex align-items-center gap-2">
                    <i class="bi ${icon}" aria-hidden="true"></i>
                    <span></span>
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="关闭"></button>
            </div>`;
        element.querySelector(".toast-body span").textContent = message;
        container.appendChild(element);

        if (window.bootstrap?.Toast) {
            const instance = new bootstrap.Toast(element, { delay: 3200 });
            element.addEventListener("hidden.bs.toast", () => element.remove(), { once: true });
            instance.show();
        } else {
            element.classList.add("show");
            window.setTimeout(() => element.remove(), 3200);
        }
    }

    window.appTheme = { apply: applyTheme, current: currentTheme };
    window.appToast = toast;

    function useProductPlaceholder(image) {
        if (!(image instanceof HTMLImageElement)) return;

        const source = image.getAttribute("src") || "";
        if (image.dataset.fallbackStage === "placeholder" && /\/images\/product-placeholder\.svg$/i.test(source)) return;
        const demoNames = ["phone", "keyboard", "coffee", "headphone", "monitor", "bottle", "backpack", "lamp", "tea", "mouse"];
        const demoMatch = source.match(/\/images\/demo-(phone|keyboard|coffee|headphone|monitor|bottle|backpack|lamp|tea|mouse)(?:-[^/.]+)?\.(?:jpg|jpeg|png)$/i);
        const bulkMatch = source.match(/\/images\/demo-bulk-product-(\d+)\.(?:jpg|jpeg|png)$/i);
        const categoryMatch = source.match(/\/images\/category-(digital|phone|computer|food|coffee|home|daily|travel)\.(?:jpg|jpeg|png|svg)$/i);
        const categoryImages = {
            digital: "monitor",
            phone: "phone",
            computer: "keyboard",
            food: "tea",
            coffee: "coffee",
            home: "lamp",
            daily: "bottle",
            travel: "backpack"
        };

        let localFallback = null;
        if (demoMatch) {
            localFallback = `/images/demo-${demoMatch[1].toLowerCase()}.png`;
        } else if (bulkMatch) {
            const index = Math.max(0, Number.parseInt(bulkMatch[1], 10) - 1) % demoNames.length;
            localFallback = `/images/demo-${demoNames[index]}.png`;
        } else if (categoryMatch) {
            localFallback = `/images/demo-${categoryImages[categoryMatch[1].toLowerCase()]}.png`;
        }

        if (localFallback && source.toLowerCase() !== localFallback.toLowerCase()) {
            image.dataset.fallbackStage = "local";
            image.src = localFallback;
            return;
        }

        image.dataset.fallbackStage = "placeholder";
        image.src = "/images/product-placeholder.svg";
    }

    document.addEventListener("error", event => {
        if (event.target instanceof HTMLImageElement) useProductPlaceholder(event.target);
    }, true);

    document.addEventListener("DOMContentLoaded", () => {
        updateThemeControls(currentTheme());
        document.querySelectorAll("[data-theme-toggle]").forEach(button => {
            button.addEventListener("click", () => {
                applyTheme(currentTheme() === "dark" ? "light" : "dark", true);
            });
        });
        document.querySelectorAll("img").forEach(image => {
            if (image.complete && image.naturalWidth === 0) useProductPlaceholder(image);
        });
    });

    const handleSystemTheme = event => {
        try {
            if (!localStorage.getItem(storageKey)) {
                applyTheme(event.matches ? "dark" : "light", false);
            }
        } catch (_) {
            applyTheme(event.matches ? "dark" : "light", false);
        }
    };
    if (mediaQuery.addEventListener) {
        mediaQuery.addEventListener("change", handleSystemTheme);
    } else if (mediaQuery.addListener) {
        mediaQuery.addListener(handleSystemTheme);
    }
})();
