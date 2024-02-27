using IVSoftware.Portable.Diagnosics;
using IVSoftware.Portable.Xml.Linq;
using System.Collections.Specialized;

namespace test_ivsoftware_portable_disposable
{
    [TestClass]
    public class Test_Disposables
    {
        [TestMethod]
        public async Task TestMethod1()
        {
            CardCollection cc = new CardCollection();
            int eventCount = 0;
            try
            {
                cc.CollectionChanged += localOnCollectionChanged;
                await localAddItemsAsync();
                Assert.AreEqual(21, eventCount);
            }
            catch (Exception ex)
            {
                Debug.Fail(ex.Message);
            }
            finally
            {
                cc.CollectionChanged -= localOnCollectionChanged;
            }   
            void localOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
            {
                eventCount++;
            }

            async Task localAddItemsAsync()
            {
                for (int i = 0; i < 10; i++)
                {
                    try
                    {
                        cc.Insert(0, CardModel.New($"Item {i + 1}", i));
                    }
                    catch (Exception ex)
                    {
                        Debug.Fail(ex.Message);
                    }
                    await Task.Delay(1000);
                }
                cc.Clear();
            }
        }
    }
}