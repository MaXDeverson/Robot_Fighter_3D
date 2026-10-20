using System.Collections;
using System.Collections.Generic;
using Game.Events;
using UnityEngine;
using Zenject;

public class AdmobInit : MonoBehaviour
{
    [Inject] private IEventPublisher _events;

    // Start is called before the first frame update
    void Start()
    {
        MyMobileAds.Init(_events);
    }

}
