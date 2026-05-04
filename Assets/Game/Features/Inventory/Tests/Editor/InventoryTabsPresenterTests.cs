using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Inventory.API;
using Inventory.Implementation.UI;
using NUnit.Framework;
using R3;
using Rewards;

namespace Inventory.Tests.Editor
{
    public sealed class InventoryTabsPresenterTests
    {
        [Test]
        public void InitializeAsync_RequestsForegroundRefresh_BeforeLoadingItems_WhenDirty()
        {
            var executionOrder = new List<string>();
            var inventoryReadService = new StubInventoryReadService(executionOrder);
            var refreshCoordinator = new StubRewardPlayerStateRefreshCoordinator(executionOrder)
            {
                HasPendingRefresh = true
            };
            var presenter = new InventoryTabsPresenter(
                "player-1",
                new StubInventoryService(),
                inventoryReadService,
                new StubItemCategoryRegistry(),
                refreshCoordinator);

            presenter.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(refreshCoordinator.ForegroundRequestCalls, Is.EqualTo(1));
            Assert.That(executionOrder, Is.EqualTo(new[] { "refresh", "load" }));
        }

        [Test]
        public void InitializeAsync_DoesNotRefresh_WhenDirtyFlagIsNotSet()
        {
            var executionOrder = new List<string>();
            var inventoryReadService = new StubInventoryReadService(executionOrder);
            var refreshCoordinator = new StubRewardPlayerStateRefreshCoordinator(executionOrder);
            var presenter = new InventoryTabsPresenter(
                "player-1",
                new StubInventoryService(),
                inventoryReadService,
                new StubItemCategoryRegistry(),
                refreshCoordinator);

            presenter.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(refreshCoordinator.ForegroundRequestCalls, Is.EqualTo(0));
            Assert.That(executionOrder, Is.EqualTo(new[] { "load" }));
        }

        private sealed class StubRewardPlayerStateRefreshCoordinator : IRewardPlayerStateRefreshCoordinator
        {
            private readonly List<string> _executionOrder;

            public StubRewardPlayerStateRefreshCoordinator(List<string> executionOrder)
            {
                _executionOrder = executionOrder;
            }

            public bool HasPendingRefresh { get; set; }
            public int ForegroundRequestCalls { get; private set; }

            public void RequestBackgroundRefresh()
            {
            }

            public UniTask RequestForegroundRefreshAsync(CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                ForegroundRequestCalls++;
                HasPendingRefresh = false;
                _executionOrder.Add("refresh");
                return UniTask.CompletedTask;
            }
        }

        private sealed class StubInventoryReadService : IInventoryReadService
        {
            private readonly List<string> _executionOrder;

            public StubInventoryReadService(List<string> executionOrder)
            {
                _executionOrder = executionOrder;
            }

            public UniTask<IReadOnlyList<InventoryItemView>> GetItemsAsync(
                string ownerId,
                string categoryId,
                CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                _executionOrder.Add("load");
                return UniTask.FromResult<IReadOnlyList<InventoryItemView>>(Array.Empty<InventoryItemView>());
            }
        }

        private sealed class StubInventoryService : IInventoryService
        {
            public Observable<InventoryChangedEvent> OnInventoryChanged { get; } = new Subject<InventoryChangedEvent>();

            public UniTask AddItemAsync(InventoryItemDelta itemDelta, CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public UniTask RemoveItemAsync(InventoryItemDelta itemDelta, CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public UniTask<InventoryBatchRemoveResult> RemoveItemsAsync(
                IReadOnlyList<InventoryItemDelta> itemDeltas,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }
        }

        private sealed class StubItemCategoryRegistry : IItemCategoryRegistry
        {
            private readonly IReadOnlyList<ItemCategory> _categories = new[]
            {
                new StubItemCategory("regular")
            };

            public void Register(ItemCategory category)
            {
            }

            public void RemoveRegister(ItemCategory category)
            {
            }

            public IReadOnlyList<ItemCategory> GetAllCategories()
            {
                return _categories;
            }

            public ItemCategory GetById(string categoryId)
            {
                return _categories[0];
            }
        }

        private sealed class StubItemCategory : ItemCategory
        {
            public StubItemCategory(string categoryId) : base(categoryId)
            {
            }

            public override CategoryUiMetadata GetMetadata()
            {
                return new ActionWidgetMetadata();
            }
        }
    }
}
