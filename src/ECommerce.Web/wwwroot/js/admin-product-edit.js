(function () {
    const { createApp, ref, computed, onMounted } = Vue;

    createApp({
        setup() {
            const submitting = ref(false);
            const loading = ref(true);
            const categoryTree = ref([]);
            const skus = ref([]);
            const images = ref([]);

            // 商品的规格定义列表（对应 ProductSpec 表）
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
                selections: {},
                price: null,
                stock: null,
                originalPrice: null,
                warningStock: 0
            });

            let skuModal = null;

            const productId = parseInt(window.location.pathname.split('/')[3]);

            // 按规格名分组的可选值
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
                return Object.entries(groups).map(([name, values]) => ({
                    specName: name,
                    values: values
                }));
            });

            // 扁平化分类树，仅返回叶子节点（无子分类的节点），带完整路径
            const leafCategories = computed(() => {
                const result = [];
                function traverse(nodes, parentPath) {
                    for (const node of nodes) {
                        const currentPath = parentPath ? parentPath + ' > ' + node.name : node.name;
                        const hasChildren = node.children && node.children.length > 0;
                        if (!hasChildren) {
                            result.push({
                                categoryId: node.categoryId,
                                name: node.name,
                                path: currentPath
                            });
                        } else {
                            traverse(node.children, currentPath);
                        }
                    }
                }
                traverse(categoryTree.value, '');
                return result;
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

                        // 加载已有规格定义
                        specs.value = (data.specs || []).map(s => ({
                            specName: s.specName,
                            specValue: s.specValue,
                            sortOrder: s.sortOrder
                        }));
                        // 当前页面暂不提供图库编辑控件；仍须回传已有图片，避免编辑其他字段时误删图库。
                        images.value = (data.images || []).map(image => ({
                            imageUrl: image.imageUrl,
                            sortOrder: image.sortOrder
                        }));

                        // 加载已有 SKU，将 specDescJson 解析回 selections
                        skus.value = (data.skus || []).map(sku => {
                            let selections = {};
                            try {
                                selections = JSON.parse(sku.specDescJson);
                            } catch {
                                // 旧数据兼容：跳过
                            }
                            return {
                                skuId: sku.skuId,
                                selections: selections,
                                specDescText: formatSkuSpec(selections),
                                price: sku.price,
                                stock: sku.stock,
                                originalPrice: sku.originalPrice,
                                warningStock: sku.warningStock,
                                skuImage: sku.skuImage,
                                status: sku.status ?? 1
                            };
                        });
                    }
                } catch (error) {
                    console.error('加载商品失败:', error.message);
                } finally {
                    loading.value = false;
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
                for (const sku of skus.value) {
                    if (sku.selections && sku.selections[removed.specName]) {
                        delete sku.selections[removed.specName];
                        sku.specDescText = formatSkuSpec(sku.selections);
                    }
                }
            }

            // ===== SKU 管理 =====
            function addSku() {
                if (specGroups.value.length === 0) {
                    alert('请先添加商品规格定义，再创建SKU');
                    return;
                }
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

            function formatSkuSpec(selections) {
                if (!selections) return '-';
                return Object.entries(selections)
                    .filter(([_, v]) => v)
                    .map(([k, v]) => `${k}:${v}`)
                    .join(' / ');
            }

            function confirmAddSku() {
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
                    status: 1,
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
                            images: images.value.map(image => ({
                                imageUrl: image.imageUrl,
                                sortOrder: image.sortOrder
                            })),
                            specs: specs.value.map((s, i) => ({
                                specName: s.specName,
                                specValue: s.specValue,
                                sortOrder: i
                            })),
                            skus: skus.value.map(sku => ({
                                skuId: sku.skuId || null,
                                specSelections: Object.entries(sku.selections || {})
                                    .filter(([_, v]) => v)
                                    .map(([k, v]) => ({ specName: k, specValue: v })),
                                price: sku.price,
                                originalPrice: sku.originalPrice,
                                stock: sku.stock,
                                warningStock: sku.warningStock,
                                skuImage: sku.skuImage || null,
                                status: sku.status ?? 1
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
                leafCategories,
                form,
                skus,
                images,
                specs,
                specForm,
                specGroups,
                skuForm,
                loadCategories,
                loadProduct,
                addSpec,
                removeSpec,
                addSku,
                removeSku,
                confirmAddSku,
                formatSkuSpec,
                submitForm
            };
        }
    }).mount('#editProductApp');
})();
