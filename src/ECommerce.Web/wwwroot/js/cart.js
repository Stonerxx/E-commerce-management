(function () {
    const { createApp } = Vue;

    createApp({
        data() {
            return {
                loading: true,
                errorMessage: "",
                items: [],
                pendingItemIds: [],
                bulkUpdating: false
            };
        },
        computed: {
            allSelected() {
                return this.items.length > 0 && this.items.every(item => item.selected);
            },
            selectedItems() {
                return this.items.filter(item => item.selected);
            },
            selectedCount() {
                return this.selectedItems.length;
            },
            selectedTotal() {
                return this.selectedItems.reduce((sum, item) => sum + Number(item.unitPrice || 0) * Number(item.quantity || 0), 0);
            }
        },
        mounted() {
            this.loadCart();
        },
        methods: {
            async loadCart() {
                this.loading = true;
                this.errorMessage = "";
                try {
                    const payload = await this.request("/api/v1/cart");
                    this.items = payload.data?.items || [];
                } catch (error) {
                    this.errorMessage = error instanceof Error ? error.message : "购物车加载失败";
                } finally {
                    this.loading = false;
                }
            },
            isBusy(cartItemId) {
                return this.bulkUpdating || this.pendingItemIds.includes(cartItemId);
            },
            setBusy(cartItemId, busy) {
                this.pendingItemIds = busy
                    ? [...new Set([...this.pendingItemIds, cartItemId])]
                    : this.pendingItemIds.filter(id => id !== cartItemId);
            },
            normalizeQuantity(item) {
                const quantity = Math.floor(Number(item.quantity));
                item.quantity = Number.isFinite(quantity) && quantity >= 1 ? quantity : 1;
            },
            async updateItem(item, quiet = false) {
                if (this.isBusy(item.cartItemId)) return;
                this.normalizeQuantity(item);
                this.setBusy(item.cartItemId, true);
                try {
                    await this.request(`/api/v1/cart/items/${item.cartItemId}`, "PUT", {
                        quantity: item.quantity,
                        selected: item.selected
                    });
                    if (!quiet) window.appToast?.("购物车已更新", "success");
                } catch (error) {
                    window.appToast?.(error instanceof Error ? error.message : "购物车更新失败", "danger");
                    await this.loadCart();
                } finally {
                    this.setBusy(item.cartItemId, false);
                }
            },
            async toggleAll(selected) {
                const changed = this.items.filter(item => item.selected !== selected);
                if (!changed.length) return;
                this.bulkUpdating = true;
                changed.forEach(item => { item.selected = selected; });
                try {
                    await Promise.all(changed.map(item => this.request(`/api/v1/cart/items/${item.cartItemId}`, "PUT", {
                        quantity: item.quantity,
                        selected: item.selected
                    })));
                    window.appToast?.(selected ? "已选择全部商品" : "已取消全选", "success");
                } catch (error) {
                    window.appToast?.(error instanceof Error ? error.message : "全选状态更新失败", "danger");
                    await this.loadCart();
                } finally {
                    this.bulkUpdating = false;
                }
            },
            async changeQuantity(item, delta) {
                if (this.isBusy(item.cartItemId)) return;
                this.normalizeQuantity(item);
                item.quantity = Math.max(1, item.quantity + delta);
                await this.updateItem(item, true);
            },
            async removeItem(item) {
                if (!window.confirm(`确认从购物车移除“${item.productName}”吗？`)) return;
                this.setBusy(item.cartItemId, true);
                try {
                    await this.request(`/api/v1/cart/items/${item.cartItemId}`, "DELETE");
                    this.items = this.items.filter(current => current.cartItemId !== item.cartItemId);
                    window.appToast?.("商品已移除", "success");
                } catch (error) {
                    window.appToast?.(error instanceof Error ? error.message : "删除失败", "danger");
                } finally {
                    this.setBusy(item.cartItemId, false);
                }
            },
            async clearCart() {
                if (!window.confirm("确认清空购物车吗？此操作无法撤销。")) return;
                this.bulkUpdating = true;
                try {
                    await this.request("/api/v1/cart", "DELETE");
                    this.items = [];
                    window.appToast?.("购物车已清空", "success");
                } catch (error) {
                    window.appToast?.(error instanceof Error ? error.message : "清空失败", "danger");
                } finally {
                    this.bulkUpdating = false;
                }
            },
            async checkout() {
                const selectedIds = this.selectedItems.map(item => item.cartItemId);
                if (!selectedIds.length) {
                    window.appToast?.("请至少选择一件商品", "warning");
                    return;
                }

                try {
                    const payload = await this.request("/api/v1/addresses");
                    if (!Array.isArray(payload.data) || payload.data.length === 0) {
                        window.appToast?.("请先添加收货地址后再结算", "warning");
                        return;
                    }
                } catch (error) {
                    window.appToast?.(error instanceof Error ? error.message : "收货地址检查失败", "danger");
                    return;
                }

                const params = new URLSearchParams();
                selectedIds.forEach(id => params.append("cartItemIds", id));
                window.location.href = `/orders/create?${params}`;
            },
            formatSpecs(value) {
                if (!value) return "默认规格";
                try {
                    const parsed = JSON.parse(value);
                    return Object.entries(parsed).map(([key, item]) => `${key}：${item}`).join(" · ") || "默认规格";
                } catch (_) {
                    return value;
                }
            },
            formatMoney(value) {
                return Number(value || 0).toFixed(2);
            },
            async request(url, method = "GET", body = null) {
                const response = await fetch(url, {
                    method,
                    headers: { Accept: "application/json", ...(body ? { "Content-Type": "application/json" } : {}) },
                    body: body ? JSON.stringify(body) : null
                });
                const payload = await response.json().catch(() => null);
                if (!response.ok || !payload?.success) throw new Error(payload?.message || `请求失败（${response.status}）`);
                return payload;
            }
        }
    }).mount("#cartApp");
})();
