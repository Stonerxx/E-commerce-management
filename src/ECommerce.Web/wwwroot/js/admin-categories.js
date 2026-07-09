(function () {
    const { createApp, ref, computed, onMounted, defineComponent, h } = Vue;

    const CategoryNode = defineComponent({
        name: 'CategoryNode',
        props: {
            node: {
                type: Object,
                required: true
            }
        },
        emits: ['create-child', 'edit', 'delete'],
        setup(props, { emit }) {
            const hasChildren = computed(() => {
                return props.node.children && props.node.children.length > 0;
            });

            function toggleExpand() {
                props.node.expanded = !props.node.expanded;
            }

            function onCreateChild() {
                emit('create-child', props.node);
            }

            function onEdit() {
                emit('edit', props.node);
            }

            function onDelete() {
                emit('delete', props.node);
            }

            return {
                hasChildren,
                toggleExpand,
                onCreateChild,
                onEdit,
                onDelete
            };
        },
        template: `
            <div>
                <div class="list-group-item px-0 border-0 d-flex align-items-center gap-2"
                     :style="{ paddingLeft: (node.treeLevel - 1) * 24 + 'px' }">
                    <span class="text-muted" style="width: 20px;">
                        <span v-if="hasChildren"
                              @click="toggleExpand"
                              class="text-primary"
                              style="cursor: pointer; user-select: none;">
                            {{ node.expanded ? '▼' : '▶' }}
                        </span>
                    </span>
                    <span class="fw-medium flex-grow-1">{{ node.name }}</span>
                    <span class="badge" :class="node.status === 1 ? 'text-bg-success' : 'text-bg-secondary'">
                        {{ node.status === 1 ? '启用' : '禁用' }}
                    </span>
                    <span class="text-muted small">第{{ node.treeLevel }}级</span>
                    <button class="btn btn-sm btn-outline-primary" @click="onCreateChild">新增子分类</button>
                    <button class="btn btn-sm btn-outline-secondary" @click="onEdit">编辑</button>
                    <button class="btn btn-sm btn-outline-danger" @click="onDelete">删除</button>
                </div>
                <div v-if="hasChildren && node.expanded">
                    <category-node v-for="child in node.children"
                                   :key="child.categoryId"
                                   :node="child"
                                   @create-child="(n) => emit('create-child', n)"
                                   @edit="(n) => emit('edit', n)"
                                   @delete="(n) => emit('delete', n)">
                    </category-node>
                </div>
            </div>
        `
    });

    createApp({
        components: {
            CategoryNode
        },
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

            function indentText(level) {
                return '　'.repeat(Math.max(0, level - 1));
            }

            const mockCategories = [
                {
                    categoryId: 1,
                    parentId: null,
                    name: '电子产品',
                    treeLevel: 1,
                    sortOrder: 0,
                    status: 1,
                    iconUrl: null,
                    expanded: true,
                    children: [
                        {
                            categoryId: 2,
                            parentId: 1,
                            name: '手机',
                            treeLevel: 2,
                            sortOrder: 0,
                            status: 1,
                            iconUrl: null,
                            expanded: true,
                            children: [
                                {
                                    categoryId: 3,
                                    parentId: 2,
                                    name: '智能手机',
                                    treeLevel: 3,
                                    sortOrder: 0,
                                    status: 1,
                                    iconUrl: null,
                                    expanded: false,
                                    children: []
                                },
                                {
                                    categoryId: 4,
                                    parentId: 2,
                                    name: '功能机',
                                    treeLevel: 3,
                                    sortOrder: 1,
                                    status: 0,
                                    iconUrl: null,
                                    expanded: false,
                                    children: []
                                }
                            ]
                        },
                        {
                            categoryId: 5,
                            parentId: 1,
                            name: '电脑',
                            treeLevel: 2,
                            sortOrder: 1,
                            status: 1,
                            iconUrl: null,
                            expanded: false,
                            children: []
                        }
                    ]
                },
                {
                    categoryId: 6,
                    parentId: null,
                    name: '服装',
                    treeLevel: 1,
                    sortOrder: 1,
                    status: 1,
                    iconUrl: null,
                    expanded: false,
                    children: []
                },
                {
                    categoryId: 7,
                    parentId: null,
                    name: '食品',
                    treeLevel: 1,
                    sortOrder: 2,
                    status: 0,
                    iconUrl: null,
                    expanded: false,
                    children: []
                }
            ];

            async function loadCategories() {
                loading.value = true;
                try {
                    const response = await fetch('/api/v1/admin/categories', {
                        headers: { 'Accept': 'application/json' }
                    });
                    const payload = await response.json();

                    if (payload.success && payload.data) {
                        treeData.value = filterTree(payload.data, showDisabled.value);
                    } else {
                        console.warn('API返回失败，使用mock数据');
                        treeData.value = filterTree(mockCategories, showDisabled.value);
                    }
                } catch (error) {
                    console.warn('API请求失败，使用mock数据:', error.message);
                    treeData.value = filterTree(mockCategories, showDisabled.value);
                } finally {
                    loading.value = false;
                }
            }

            function resetForm() {
                form.value = {
                    name: '',
                    parentId: null,
                    sortOrder: 0,
                    iconUrl: '',
                    status: 1
                };
                errorMsg.value = '';
                isEdit.value = false;
                editingId.value = null;
            }

            function openCreateModal(parentNode) {
                resetForm();
                if (parentNode) {
                    form.value.parentId = parentNode.categoryId;
                }
                showModal();
            }

            function openEditModal(node) {
                resetForm();
                isEdit.value = true;
                editingId.value = node.categoryId;
                form.value.name = node.name;
                form.value.parentId = node.parentId;
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

            onMounted(() => {
                loadCategories();
            });

            return {
                loading,
                showDisabled,
                treeData,
                parentOptions,
                isEdit,
                submitting,
                errorMsg,
                form,
                indentText,
                loadCategories,
                openCreateModal,
                openEditModal,
                saveCategory,
                deleteCategory
            };
        }
    }).mount('#categoriesApp');
})();
