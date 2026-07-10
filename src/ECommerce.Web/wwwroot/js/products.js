(function () {
    const { createApp, ref, computed, onMounted } = Vue;

    createApp({
        setup() {
            const loading = ref(false);
            const keyword = ref('');
            const selectedCategoryId = ref(null);
            const categoryTree = ref([]);
            const products = ref([]);
            const pagination = ref({
                pageIndex: 1,
                pageSize: 12,
                totalCount: 0,
                totalPages: 0
            });

            const pageNumbers = computed(() => {
                const pages = [];
                const total = pagination.value.totalPages;
                const current = pagination.value.pageIndex;
                const maxVisible = 5;
                let start = Math.max(1, current - 2);
                let end = Math.min(total, start + maxVisible - 1);
                if (end - start + 1 < maxVisible) {
                    start = Math.max(1, end - maxVisible + 1);
                }
                for (let i = start; i <= end; i++) {
                    pages.push(i);
                }
                return pages;
            });

            async function loadCategories() {
                try {
                    const response = await fetch('/api/v1/categories', {
                        headers: { 'Accept': 'application/json' }
                    });
                    const result = await response.json();
                    if (result.success && result.data) {
                        categoryTree.value = result.data;
                    }
                } catch (error) {
                    console.error('加载分类失败:', error);
                }
            }

            async function loadProducts(page) {
                const pageIndex = page || 1;
                loading.value = true;

                const params = new URLSearchParams({
                    pageIndex: pageIndex,
                    pageSize: pagination.value.pageSize,
                    status: 1
                });
                if (keyword.value.trim()) {
                    params.append('keyword', keyword.value.trim());
                }
                if (selectedCategoryId.value) {
                    params.append('categoryId', selectedCategoryId.value);
                }

                try {
                    const response = await fetch(`/api/v1/products?${params.toString()}`, {
                        headers: { 'Accept': 'application/json' }
                    });
                    const result = await response.json();

                    if (result.success && result.data) {
                        products.value = result.data.items || [];
                        pagination.value = {
                            pageIndex: result.data.pageIndex,
                            pageSize: result.data.pageSize,
                            totalCount: result.data.totalCount,
                            totalPages: result.data.totalPages || 0
                        };
                    } else {
                        products.value = [];
                    }
                } catch (error) {
                    console.error('加载商品失败:', error);
                    products.value = [];
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

            onMounted(() => {
                loadCategories();
                loadProducts(1);
            });

            return {
                loading,
                keyword,
                selectedCategoryId,
                categoryTree,
                products,
                pagination,
                pageNumbers,
                loadProducts,
                selectCategory,
                clearCategory
            };
        }
    }).mount('#productsApp');
})();
