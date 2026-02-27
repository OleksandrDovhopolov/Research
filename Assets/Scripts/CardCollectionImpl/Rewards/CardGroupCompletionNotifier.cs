using System;
using System.Collections;
using System.Collections.Generic;
using CardCollection.Core;
using UnityEngine;

namespace core
{
    public class CardGroupCompletionNotifier : ICardGroupCompletionNotifier
    {
        public event Action<CardGroupCompletedData> OnGroupCompleted;
    }
}