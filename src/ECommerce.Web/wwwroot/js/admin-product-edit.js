(function () {
    const { createApp, ref, onMounted } = Vue;

    createApp({
        setup() {
            const submitting = ref(false);
            const loading = ref(true);
            const categoryTree = ref([]);
            const skus = ref([]);

            const form = ref({
                name: '',
                categoryId: null,
                description: '',
                mainImage: '',
                status: 1
            });

            const skuForm = ref({
                specDesc: '',
                price: null,
                stock: null,
                originalPrice: null,
                warningStock: 0
            });

            let skuModal = null;

            const productId = parseInt(window.location.pathname.split('/')[3]);

            async function loadCategories() {
                try {
                    const response = await fetch('/api/v1/admin/categories', {
                        headers: { 'Accept': 'application/json' }
                    });
                    const payload = await response.json();
                    if (payload.success && payload.data) {
                        categoryTree.value = payload.data;
                    }
                } catch (error) {
                    console.warn('加载分类失败:', error.message);
                }
            }

            async function loadProduct() {
                try {
                    const response = await fetch(`/api/v1/admin/products/${productId}`, {
                        headers: { 'Accept': 'application/json' }
                    });
                    const payload = await response.json();
                    if (payload.success && payload.data) {
                        const data = payload.data;
                        form.value = {
                            name: data.name,
                            categoryId: data.categoryId,
                            description: data.description || '',
                            mainImage: data.mainImage || '',
                            status: data.status
                        };
                        skus.value = (data.skus || []).map(sku => {
                            let specDesc = sku.specDescJson;
                            try {
                                const parsed = JSON.parse(sku.specDescJson);
                                specDesc = Object.entries(parsed).map(([k, v]) => `${k}:${v}`).join(',');
                            } catch {
                            }
                            return {
                                skuId: sku.skuId,
                                specDesc: specDesc,
                                price: sku.price,
                                stock: sku.stock,
                                originalPrice: sku.originalPrice,
                                warningStock: sku.warningStock,
                                skuImage: sku.skuImage,
                                status: sku.status
                            };
                        });
                    }
                } catch (error) {
                    console.error('加载商品失败:', error.message);
                } finally {
                    loading.value = false;
                }
            }

            function addSku() {
                skuForm.value = {
                    specDesc: '',
                    price: null,
                    stock: null,
                    originalPrice: null,
                    warningStock: 0
                };
                if (!skuModal) {
                    const modalEl = document.getElementById('skuModal');
                    skuModal = new bootstrap.Modal(modalEl);
                }
                skuModal.show();
            }

            function removeSku(index) {
                skus.value.splice(index, 1);
            }

            function confirmAddSku() {
                if (!skuForm.value.specDesc || !skuForm.value.price || !skuForm.value.stock) {
                    alert('请填写规格描述、价格和库存');
                    return;
                }

                skus.value.push({
                    specDesc: skuForm.value.specDesc,
                    price: skuForm.value.price,
                    stock: skuForm.value.stock,
                    originalPrice: skuForm.value.originalPrice,
                    warningStock: skuForm.value.warningStock || 0
                });

                skuModal.hide();
            }

            async function submitForm() {
                if (!form.value.name || !form.value.categoryId || !form.value.mainImage) {
                    alert('请填写商品名称、分类和主图');
                    return;
                }

                submitting.value = true;

                try {
                    const response = await fetch(`/api/v1/admin/products/${productId}`, {
                        method: 'PUT',
                        headers: {
                            'Content-Type': 'application/json',
                            'Accept': 'application/json'
                        },
                        body: JSON.stringify({
                            name: form.value.name.trim(),
                            categoryId: form.value.categoryId,
                            description: form.value.description || null,
                            mainImage: form.value.mainImage,
                            status: form.value.status,
                            images: [],
                            specs: [],
                            skus: skus.value.map(sku => ({
                                specDescJson: typeof sku.specDesc === 'object' ? JSON.stringify(sku.specDesc) : JSON.stringify({ desc: sku.specDesc }),
                                price: sku.price,
                                originalPrice: sku.originalPrice,
                                stock: sku.stock,
                                warningStock: sku.warningStock,
                                skuImage: sku.skuImage || null,
                                status: sku.status || 1
                            }))
                        })
                    });

                    const payload = await response.json();

                    if (payload.success) {
                        alert('商品更新成功');
                        window.location.href = '/admin/products';
                    } else {
                        alert(payload.message || '更新失败');
                    }
                } catch (error) {
                    alert(error.message || '网络错误');
                } finally {
                    submitting.value = false;
                }
            }

            onMounted(() => {
                loadCategories();
                loadProduct();
            });

            return {
                submitting,
                loading,
                categoryTree,
                form,
                skus,
                skuForm,
                loadCategories,
                loadProduct,
                addSku,
                removeSku,
                confirmAddSku,
                submitForm
            };
        }
    }).mount('#editProductApp');
})();