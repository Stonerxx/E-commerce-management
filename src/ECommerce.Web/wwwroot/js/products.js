(function () {
    const { createApp, ref, computed, onMounted } = Vue;
    const initialParams = new URLSearchParams(window.location.search);

    createApp({
        setup() {
            const loading = ref(false);
            const errorMessage = ref("");
            const keyword = ref(initialParams.get("keyword") || "");
            const categoryValue = Number(initialParams.get("categoryId"));
            const selectedCategoryId = ref(Number.isInteger(categoryValue) && categoryValue > 0 ? categoryValue : null);
            const allowedSorts = ["newest", "hot", "sales", "rating", "priceAsc", "priceDesc"];
            const requestedSort = initialParams.get("sortBy") || "newest";
            const sortBy = ref(allowedSorts.includes(requestedSort) ? requestedSort : "newest");
            const categoryTree = ref([]);
            const products = ref([]);
            const pagination = ref({ pageIndex: 1, pageSize: 12, totalCount: 0, totalPages: 0 });

            const pageNumbers = computed(() => {
                const pages = [];
                const total = pagination.value.totalPages;
                const current = pagination.value.pageIndex;
                let start = Math.max(1, current - 2);
                let end = Math.min(total, start + 4);
                if (end - start < 4) start = Math.max(1, end - 4);
                for (let page = start; page <= end; page += 1) pages.push(page);
                return pages;
            });

            async function loadCategories() {
                try {
                    const response = await fetch("/api/v1/categories", { headers: { Accept: "application/json" } });
                    const result = await response.json().catch(() => null);
                    categoryTree.value = response.ok && result?.success ? (result.data || []) : [];
                } catch (error) {
                    console.error("加载分类失败:", error);
                }
            }

            async function loadProducts(page = 1) {
                if (page < 1 || (pagination.value.totalPages > 0 && page > pagination.value.totalPages)) return;
                loading.value = true;
                errorMessage.value = "";
                const params = new URLSearchParams({
                    pageIndex: String(page),
                    pageSize: String(pagination.value.pageSize),
                    sortBy: sortBy.value
                });
                if (keyword.value.trim()) params.set("keyword", keyword.value.trim());
                if (selectedCategoryId.value) params.set("categoryId", String(selectedCategoryId.value));

                try {
                    const response = await fetch(`/api/v1/products?${params}`, { headers: { Accept: "application/json" } });
                    const result = await response.json().catch(() => null);
                    if (!response.ok || !result?.success) throw new Error(result?.message || "商品加载失败");

                    products.value = result.data?.items || [];
                    pagination.value = {
                        pageIndex: result.data?.pageIndex || page,
                        pageSize: result.data?.pageSize || pagination.value.pageSize,
                        totalCount: result.data?.totalCount || 0,
                        totalPages: result.data?.totalPages || 0
                    };

                    const visibleParams = new URLSearchParams();
                    if (keyword.value.trim()) visibleParams.set("keyword", keyword.value.trim());
                    if (selectedCategoryId.value) visibleParams.set("categoryId", String(selectedCategoryId.value));
                    if (sortBy.value !== "newest") visibleParams.set("sortBy", sortBy.value);
                    if (page > 1) visibleParams.set("page", String(page));
                    history.replaceState(null, "", `${window.location.pathname}${visibleParams.size ? `?${visibleParams}` : ""}`);
                } catch (error) {
                    products.value = [];
                    errorMessage.value = error instanceof Error ? error.message : "商品加载失败，请稍后重试";
                } finally {
                    loading.value = false;
                }
            }

            function selectCategory(categoryId) {
                selectedCategoryId.value = categoryId;
                loadProducts(1);
            }

            function clearCategory() {
                selectedCategoryId.value = null;
                loadProducts(1);
            }

            function resetFilters() {
                keyword.value = "";
                selectedCategoryId.value = null;
                sortBy.value = "newest";
                loadProducts(1);
            }

            function formatMoney(value) {
                return Number(value || 0).toFixed(2);
            }

            onMounted(() => {
                loadCategories();
                loadProducts(Number(initialParams.get("page")) || 1);
            });

            return { loading, errorMessage, keyword, selectedCategoryId, sortBy, categoryTree, products, pagination, pageNumbers, loadProducts, selectCategory, clearCategory, resetFilters, formatMoney };
        }
    }).mount("#productsApp");
})();
