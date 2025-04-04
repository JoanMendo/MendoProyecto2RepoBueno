using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using System;
using UnityEngine.InputSystem.Interactions;

public class InputManager : MonoBehaviour, InputSystem_Actions.IUIActions
{
    private InputSystem_Actions inputActions;
    public Vector2 MousePosition { get; private set; } //Posicion del mouse en la pantalla

    public static event Action OnClicked; //Evento para el click izquierdo
    public static event Action<Vector2> OnMouseMoved;

    private void Awake()
    {
        inputActions = new InputSystem_Actions();
        inputActions.UI.SetCallbacks(this);
    }

    private void OnEnable()
    {
        inputActions.UI.Enable();
    }

    private void OnDisable()
    {
        inputActions.UI.Disable();
    }

    public void OnNavigate(InputAction.CallbackContext context) //WASD para moverte entre lso botones de una interfaz
    {
        throw new System.NotImplementedException();
    }

    public void OnSubmit(InputAction.CallbackContext context) //Darle a un boton para seleccionar
    {
        throw new System.NotImplementedException();
    }

    public void OnCancel(InputAction.CallbackContext context) //Darlea un boton para cancelar
    {
        throw new System.NotImplementedException();
    }

    public void OnPoint(InputAction.CallbackContext context) //Posicion del mouse en la pantalla
   
    {
        if (context.performed) //Si el mouse se mueve
        {
            MousePosition = context.ReadValue<Vector2>(); //Lee la posicion del mouse
            OnMouseMoved?.Invoke(MousePosition); //Invoca el evento OnMouseMoved si no es nulo
        }
    }

    public void OnClick(InputAction.CallbackContext context) //Con que boton se hace click
    {
        if (context.performed && !context.canceled) //Si el click es realizado
        {
            Debug.Log("Click realizado en la posici�n: " + MousePosition); //Imprime la posicion del click
            OnClicked?.Invoke(); //Invoca el evento OnClicked si no es nulo

        }


    }

    public void OnRightClick(InputAction.CallbackContext context) //Con que boton se hace click derecho

    {
        throw new System.NotImplementedException();
    }

    public void OnMiddleClick(InputAction.CallbackContext context) //Con que boton se hace click medio

    {
        throw new System.NotImplementedException();
    }

    public void OnScrollWheel(InputAction.CallbackContext context) //Al deslizar con la rueda del mouse
    {
        throw new System.NotImplementedException();
    }


}
//


