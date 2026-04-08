namespace CardCollectionImpl
{
    public interface ICardCollectionWindowCoordinator
    {
        void ShowStarted(CollectionStartedArgs args);
        void ShowCompleted(CollectionCompletedArgs args);
        void ShowCollection(CardCollectionArgs args);
        void ShowNewCard(NewCardArgs args);
        void ShowGroupCompleted(CardGroupCollectionArgs args);
        void CloseSessionWindows();
    }
}
