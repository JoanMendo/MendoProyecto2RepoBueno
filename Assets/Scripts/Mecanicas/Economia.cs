using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Economia : MonoBehaviour
{
    public float money = 0;

    public void more_money(float incremeneto)
    {
        money += incremeneto;
    }

    public void less_money(float restante)
    {
        money -= restante;
    }


}
