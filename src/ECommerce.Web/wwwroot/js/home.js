(function () {
    const root = document.getElementById("homeApp");
    if (!root || typeof Vue === "undefined") return;

    const { createApp } = Vue;
    createApp({
        data() {
            return {
                catalogLoading: false,
                catalogError: "",
                categories: [],
                hotProducts: []
            };
        },
        mounted() {
            if (root.dataset.loadCatalog === "true") {
                this.loadCatalog();
            }
        },
        methods: {
            async loadCatalog() {
                this.catalogLoading = true;
                this.catalogError = "";
                try {
                    const [productsResponse, categoriesResponse] = await Promise.all([
                        fetch("/api/v1/products?pageIndex=1&pageSize=8&sortBy=hot", { headers: { Accept: "application/json" } }),
                        fetch("/api/v1/categories", { headers: { Accept: "application/json" } })
                    ]);
                    const productsPayload = await productsResponse.json().catch(() => null);
                    const categoriesPayload = await categoriesResponse.json().catch(() => null);

                    if (!productsResponse.ok || !productsPayload?.success) {
                        throw new Error(productsPayload?.message || "推荐商品加载失败");
                    }

                    this.hotProducts = productsPayload.data?.items || [];
                    this.categories = categoriesResponse.ok && categoriesPayload?.success
                        ? (categoriesPayload.data || []).slice(0, 8)
                        : [];
                } catch (error) {
                    this.catalogError = error instanceof Error ? error.message : "推荐商品暂时无法加载";
                    this.hotProducts = [];
                } finally {
                    this.catalogLoading = false;
                }
            },
            formatMoney(value) {
                return Number(value || 0).toFixed(2);
            }
        }
    }).mount(root);
})();
