using System;
using UnityEngine;
using UnityEngine.InputSystem;
/**
* Title:
* Descrpiton:
*/

public class PlayerInputCtr : MonoBehaviour
{
   public static PlayerInputCtr Instance;
   public PlayerInput playerInput;
   

   public event Action<bool> ShiftPressedEvent;
   
   public event Action JumpingEvent;
   public event Action<string> SkillKeyEvent;
   public event Action<string> MainUIKeyHandler;
   public Vector2 Movement
   {
      get => playerInput.player.Movement.ReadValue<Vector2>();
   }

   private void Awake()
   {
      Instance = this;
      playerInput = new PlayerInput();
      RegistInputEvent();
   }

   private void RegistInputEvent()
   {
      
      playerInput.player.Shift.started += (context) =>
      {
         ShiftPressedEvent?.Invoke(true);
      };
      playerInput.player.Shift.canceled += (context) =>
      {
         ShiftPressedEvent?.Invoke(false);
      };
      playerInput.player.Jump.canceled += (context) =>
      {
         JumpingEvent?.Invoke();
      };
      Keyboard.current.onTextInput += c =>
      {
         if(!playerInput.asset.enabled) return;
         string key = c.ToString().ToUpper();
         switch (key)
         {
            case "Q":
            case "E":
            case "R":
            case "F":
            case "1":
            case "2":
            case "3":
            case "4":
            case "5":
            case "6":
            case "7":
            case "8":
            case "9":
            
               SkillKeyEvent?.Invoke(key);
               break;
            
            case "L":
            case "B":
            case "I":
               MainUIKeyHandler?.Invoke(key);
               break;
            default:
               break;
               
         }
      };
   }

   


   public void OnEnable()
   {
      playerInput.asset.Enable();
      
   }

   public void OnDisable()
   {
      if(playerInput.asset!=null) playerInput.asset.Disable();
     
      
   }

   private void OnDestroy()
   {
      if(playerInput != null) playerInput.Disable();
      
   }
}


