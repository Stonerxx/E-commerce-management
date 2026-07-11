(function () {
    const { createApp, ref, computed, onMounted } = Vue;

    createApp({
        setup() {
            const loading = ref(false);
            const products = ref([]);
            const categories = ref([]);
            const categoryOptions = ref([]);

            const keyword = ref('');
            const categoryId = ref(null);
            const status = ref(null);

            const pagination = ref({
                currentPage: 1,
                pageSize: 10,
                total: 0,
                totalPages: 0
            });

            const pageNumbers = computed(() => {
                const pages = [];
                const total = pagination.value.totalPages;
                const current = pagination.value.currentPage;
                const start = Math.max(1, current - 2);
                const end = Math.min(total, current + 2);
                for (let i = start; i <= end; i++) pages.push(i);
                return pages;
            });

            function getCategoryName(catId) {
                const cat = categories.value.find(c => c.categoryId === catId);
                return cat ? cat.name : '-';
            }

            function formatPrice(price) {
                return '¥' + Number(price).toFixed(2);
            }

            function formatRating(rating) {
                if (!rating || rating <= 0) return '-';
                return Number(rating).toFixed(1) + '分';
            }

            function getStatusText(s) {
                switch (s) {
                    case 0: return '已下架';
                    case 1: return '已上架';
                    case 2: return '预售';
                    default: return '未知';
                }
            }

            function getStatusClass(s) {
                switch (s) {
                    case 0: return 'text-bg-secondary';
                    case 1: return 'text-bg-success';
                    case 2: return 'text-bg-warning';
                    default: return 'text-bg-secondary';
                }
            }

            async function loadCategories() {
                try {
                    const response = await fetch('/api/v1/admin/categories', {
                        headers: { 'Accept': 'application/json' }
                    });
                    const payload = await response.json();
                    if (payload.success && payload.data) {
                        const flat = [];
                        function flatten(nodes) {
                            for (const node of nodes) {
                                flat.push({ categoryId: node.categoryId, name: node.name });
                                if (node.children && node.children.length > 0) flatten(node.children);
                            }
                        }
                        flatten(payload.data);
                        categories.value = flat;
                        categoryOptions.value = flat;
                    } else {
                        categories.value = [];
                        categoryOptions.value = [];
                    }
                } catch (err) {
                    console.error('加载分类失败:', err);
                }
            }

            async function loadProducts(page = 1) {
                pagination.value.currentPage = page;
                loading.value = true;
                try {
                    const params = new URLSearchParams();
                    params.set('pageIndex', page);
                    params.set('pageSize', pagination.value.pageSize);
                    if (keyword.value) params.set('keyword', keyword.value);
                    if (categoryId.value != null) params.set('categoryId', categoryId.value);
                    if (status.value != null) params.set('status', status.value);

                    const response = await fetch('/api/v1/admin/products?' + params.toString(), {
                        headers: { 'Accept': 'application/json' }
                    });
                    const payload = await response.json();
                    if (payload.success && payload.data) {
                        products.value = payload.data.items || [];
                        pagination.value.total = payload.data.totalCount || 0;
                        pagination.value.totalPages = payload.data.totalPages || 0;
                    } else {
                        products.value = [];
                        pagination.value.total = 0;
                        pagination.value.totalPages = 0;
                    }
                } catch (err) {
                    console.error('加载商品失败:', err);
                    products.value = [];
                } finally {
                    loading.value = false;
                }
            }

            async function toggleStatus(product) {
                const newStatus = product.status === 1 ? 0 : 1;
                if (!confirm(`确定要${newStatus === 1 ? '上架' : '下架'}商品"${product.name}"吗？`)) return;
                try {
                    const response = await fetch(`/api/v1/admin/products/${product.productId}/status`, {
                        method: 'PUT',
                        headers: { 'Content-Type': 'application/json', 'Accept': 'application/json' },
                        body: JSON.stringify({ status: newStatus })
                    });
                    const payload = await response.json();
                    if (payload.success) {
                        product.status = newStatus;
                    } else {
                        alert(payload.message || '操作失败');
                    }
                } catch (err) {
                    alert('网络错误：' + err.message);
                }
            }

            onMounted(() => {
                loadCategories();
                loadProducts();
            });

            return {
                loading, products, categoryOptions,
                keyword, categoryId, status,
                pagination, pageNumbers,
                getCategoryName, formatPrice, formatRating,
                getStatusText, getStatusClass,
                loadProducts, toggleStatus
            };
        }
    }).mount('#productsApp');
})();
