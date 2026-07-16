(function () {
    const { createApp, ref, computed, onMounted } = Vue;

    createApp({
        setup() {
            const submitting = ref(false);
            const errorMsg = ref('');
            const categoryTree = ref([]);
            const skus = ref([]);

            // 商品的规格定义列表（对应 ProductSpec 表）
            // 每项: { specName: '颜色', specValue: '红', sortOrder: 0 }
            const specs = ref([]);

            // 规格定义表单
            const specForm = ref({
                specName: '',
                specValue: '',
                sortOrder: 0
            });

            const form = ref({
                name: '',
                categoryId: null,
                description: '',
                mainImage: '',
                status: 1
            });

            // SKU 添加弹窗
            const skuForm = ref({
                selections: {},  // { specName: selectedValue }
                price: null,
                stock: null,
                originalPrice: null,
                warningStock: 0
            });

            let skuModal = null;

            // 按规格名分组的可选值，用于前端选择器渲染
            const specGroups = computed(() => {
                const groups = {};
                for (const spec of specs.value) {
                    if (!groups[spec.specName]) {
                        groups[spec.specName] = [];
                    }
                    if (!groups[spec.specName].includes(spec.specValue)) {
                        groups[spec.specName].push(spec.specValue);
                    }
                }
                // 返回 [{ specName, values: [...] }]
                return Object.entries(groups).map(([name, values]) => ({
                    specName: name,
                    values: values
                }));
            });

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

            // ===== 规格定义管理 =====
            function addSpec() {
                if (!specForm.value.specName.trim() || !specForm.value.specValue.trim()) {
                    alert('请填写规格名称和规格值');
                    return;
                }
                specs.value.push({
                    specName: specForm.value.specName.trim(),
                    specValue: specForm.value.specValue.trim(),
                    sortOrder: specForm.value.sortOrder || 0
                });
                specForm.value = { specName: '', specValue: '', sortOrder: specs.value.length };
            }

            function removeSpec(index) {
                const removed = specs.value[index];
                specs.value.splice(index, 1);
                // 同时移除 SKU 中使用了该规格的选择
                for (const sku of skus.value) {
                    if (sku.selections && sku.selections[removed.specName]) {
                        delete sku.selections[removed.specName];
                    }
                }
            }

            // ===== SKU 管理 =====
            function addSku() {
                if (specGroups.value.length === 0) {
                    alert('请先添加商品规格定义，再创建SKU');
                    return;
                }
                // 初始化每个规格名的选择为空
                const selections = {};
                for (const group of specGroups.value) {
                    selections[group.specName] = '';
                }
                skuForm.value = {
                    selections: selections,
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

            // 生成 SKU 的规格描述文本（用于列表展示）
            function formatSkuSpec(selections) {
                if (!selections) return '-';
                return Object.entries(selections)
                    .filter(([_, v]) => v)
                    .map(([k, v]) => `${k}:${v}`)
                    .join(' / ');
            }

            function confirmAddSku() {
                // 校验所有规格都已选择
                for (const group of specGroups.value) {
                    if (!skuForm.value.selections[group.specName]) {
                        alert(`请选择 "${group.specName}" 的规格值`);
                        return;
                    }
                }
                if (!skuForm.value.price || skuForm.value.stock === null) {
                    alert('请填写价格和库存');
                    return;
                }

                skus.value.push({
                    selections: { ...skuForm.value.selections },
                    price: skuForm.value.price,
                    stock: skuForm.value.stock,
                    originalPrice: skuForm.value.originalPrice,
                    warningStock: skuForm.value.warningStock || 0,
                    specDescText: formatSkuSpec(skuForm.value.selections)
                });

                skuModal.hide();
            }

            async function submitForm() {
                if (!form.value.name || !form.value.categoryId || !form.value.mainImage) {
                    alert('请填写商品名称、分类和主图');
                    return;
                }

                if (specs.value.length === 0) {
                    alert('请至少添加一项商品规格定义');
                    return;
                }

                if (skus.value.length === 0) {
                    alert('请至少添加一个SKU');
                    return;
                }

                submitting.value = true;
                errorMsg.value = '';

                try {
                    const response = await fetch('/api/v1/admin/products', {
                        method: 'POST',
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
                            specs: specs.value.map((s, i) => ({
                                specName: s.specName,
                                specValue: s.specValue,
                                sortOrder: i
                            })),
                            skus: skus.value.map(sku => ({
                                specSelections: Object.entries(sku.selections)
                                    .filter(([_, v]) => v)
                                    .map(([k, v]) => ({ specName: k, specValue: v })),
                                price: sku.price,
                                originalPrice: sku.originalPrice,
                                stock: sku.stock,
                                warningStock: sku.warningStock,
                                skuImage: null,
                                status: 1
                            }))
                        })
                    });

                    const payload = await response.json();

                    if (payload.success) {
                        alert('商品创建成功');
                        window.location.href = '/admin/products';
                    } else {
                        errorMsg.value = payload.message || '创建失败';
                    }
                } catch (error) {
                    errorMsg.value = error.message || '网络错误';
                } finally {
                    submitting.value = false;
                }
            }

            onMounted(() => {
                loadCategories();
            });

            return {
                submitting,
                errorMsg,
                categoryTree,
                form,
                skus,
                specs,
                specForm,
                specGroups,
                skuForm,
                loadCategories,
                addSpec,
                removeSpec,
                addSku,
                removeSku,
                confirmAddSku,
                formatSkuSpec,
                submitForm
            };
        }
    }).mount('#createProductApp');
})();
