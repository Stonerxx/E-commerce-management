(function () {
    const { createApp, ref, computed, onMounted } = Vue;

    createApp({
        setup() {
            const loading = ref(false);
            const showDisabled = ref(false);
            const treeData = ref([]);

            const isEdit = ref(false);
            const editingId = ref(null);
            const submitting = ref(false);
            const errorMsg = ref('');
            const form = ref({
                name: '',
                parentId: null,
                treeLevel: 1,
                sortOrder: 0,
                iconUrl: '',
                status: 1
            });

            let modal = null;

            function flattenTree(nodes, result) {
                for (const node of nodes) {
                    result.push(node);
                    if (node.children && node.children.length > 0) {
                        flattenTree(node.children, result);
                    }
                }
            }

            function findNodeById(nodes, id) {
                for (const node of nodes) {
                    if (node.categoryId === id) return node;
                    if (node.children && node.children.length > 0) {
                        const found = findNodeById(node.children, id);
                        if (found) return found;
                    }
                }
                return null;
            }

            function filterTree(nodes, includeDisabled) {
                const filtered = [];
                for (const node of nodes) {
                    if (includeDisabled || node.status === 1) {
                        const filteredChildren = filterTree(node.children || [], includeDisabled);
                        filtered.push({
                            ...node,
                            children: filteredChildren,
                            expanded: node.expanded !== false
                        });
                    }
                }
                return filtered;
            }

            const parentOptions = computed(() => {
                const options = [];
                flattenTree(treeData.value, options);
                return options;
            });

            const flatTreeData = computed(() => {
                const nodes = [];

                function appendVisibleNodes(items) {
                    for (const item of items) {
                        nodes.push(item);
                        if (item.expanded !== false && item.children && item.children.length > 0) {
                            appendVisibleNodes(item.children);
                        }
                    }
                }

                appendVisibleNodes(treeData.value);
                return nodes;
            });

            function findParentLevel(parentId) {
                if (!parentId) return 0;
                for (const node of parentOptions.value) {
                    if (node.categoryId === parentId) {
                        return node.treeLevel;
                    }
                }
                return 0;
            }

            function updateTreeLevel() {
                if (form.value.parentId !== null) {
                    const parentLevel = findParentLevel(form.value.parentId);
                    form.value.treeLevel = parentLevel + 1;
                } else {
                    form.value.treeLevel = 1;
                }
            }

            function indentText(level) {
                return '　'.repeat(Math.max(0, level - 1));
            }

            async function loadCategories() {
                loading.value = true;
                try {
                    const response = await fetch('/api/v1/admin/categories', {
                        headers: { 'Accept': 'application/json' }
                    });
                    const payload = await response.json();

                    if (payload.success && payload.data) {
                        treeData.value = filterTree(payload.data, showDisabled.value);
                    }
                } catch (error) {
                    console.warn('API请求失败:', error.message);
                } finally {
                    loading.value = false;
                }
            }

            function resetForm() {
                form.value = {
                    name: '',
                    parentId: null,
                    treeLevel: 1,
                    sortOrder: 0,
                    iconUrl: '',
                    status: 1
                };
                errorMsg.value = '';
                isEdit.value = false;
                editingId.value = null;
            }

            function openCreateModal(parentNode) {
                console.log('openCreateModal called with:', parentNode);
                resetForm();
                if (parentNode) {
                    form.value.parentId = parentNode.categoryId;
                    form.value.treeLevel = (parentNode.treeLevel || 1) + 1;
                }
                showModal();
            }

            function openEditModal(node) {
                resetForm();
                isEdit.value = true;
                editingId.value = node.categoryId;
                form.value.name = node.name;
                form.value.parentId = node.parentId;
                form.value.treeLevel = node.treeLevel || 1;
                form.value.sortOrder = node.sortOrder || 0;
                form.value.iconUrl = node.iconUrl || '';
                form.value.status = node.status;
                showModal();
            }

            function showModal() {
                if (!modal) {
                    const modalEl = document.getElementById('categoryModal');
                    modal = new bootstrap.Modal(modalEl);
                }
                modal.show();
            }

            function hideModal() {
                if (modal) {
                    modal.hide();
                }
            }

            async function saveCategory() {
                if (!form.value.name || form.value.name.trim() === '') {
                    errorMsg.value = '分类名称不能为空';
                    return;
                }

                submitting.value = true;
                errorMsg.value = '';

                try {
                    const url = isEdit.value
                        ? `/api/v1/admin/categories/${editingId.value}`
                        : '/api/v1/admin/categories';
                    const method = isEdit.value ? 'PUT' : 'POST';

                    const response = await fetch(url, {
                        method: method,
                        headers: {
                            'Content-Type': 'application/json',
                            'Accept': 'application/json'
                        },
                        body: JSON.stringify({
                            name: form.value.name.trim(),
                            parentId: form.value.parentId,
                            treeLevel: form.value.treeLevel,
                            sortOrder: form.value.sortOrder || 0,
                            iconUrl: form.value.iconUrl || null,
                            status: form.value.status
                        })
                    });

                    const payload = await response.json();

                    if (payload.success) {
                        hideModal();
                        await loadCategories();
                    } else {
                        errorMsg.value = payload.message || '操作失败';
                    }
                } catch (error) {
                    errorMsg.value = error.message || '网络错误';
                } finally {
                    submitting.value = false;
                }
            }

            async function deleteCategory(node) {
                if (!confirm(`确定要删除分类"${node.name}"吗？删除后无法恢复。`)) {
                    return;
                }

                try {
                    const response = await fetch(`/api/v1/admin/categories/${node.categoryId}`, {
                        method: 'DELETE',
                        headers: { 'Accept': 'application/json' }
                    });

                    const payload = await response.json();

                    if (payload.success) {
                        await loadCategories();
                    } else {
                        alert(payload.message || '删除失败');
                    }
                } catch (error) {
                    alert(error.message || '网络错误');
                }
            }

            function toggleExpand(node) {
                node.expanded = !node.expanded;
            }

            function handleButtonClick(event) {
                const target = event.currentTarget;
                const action = target.getAttribute('data-action');
                const categoryId = parseInt(target.getAttribute('data-id'));
                
                console.log('handleButtonClick:', action, categoryId);
                
                if (!isNaN(categoryId)) {
                    const node = findNodeById(treeData.value, categoryId);
                    console.log('Found node:', node);
                    if (node) {
                        switch (action) {
                            case 'create-child':
                                openCreateModal(node);
                                break;
                            case 'edit':
                                openEditModal(node);
                                break;
                            case 'delete':
                                deleteCategory(node);
                                break;
                        }
                    }
                }
            }

            function handleCreateChild(parentNode) {
                openCreateModal(parentNode);
            }

            function handleEdit(node) {
                openEditModal(node);
            }

            function handleDelete(node) {
                deleteCategory(node);
            }

            onMounted(() => {
                loadCategories();
            });

            return {
                loading,
                showDisabled,
                treeData,
                flatTreeData,
                parentOptions,
                isEdit,
                submitting,
                errorMsg,
                form,
                indentText,
                updateTreeLevel,
                loadCategories,
                openCreateModal,
                openEditModal,
                saveCategory,
                deleteCategory,
                toggleExpand,
                handleButtonClick
            };
        }
    }).mount('#categoriesApp');
})();
