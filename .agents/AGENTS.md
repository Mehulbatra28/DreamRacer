# Unity Best Practices & Preferences

- **Performance & Architecture:** Do not use `FindObjectOfType`, `GetComponent`, or similar expensive calls to automatically link dependencies at runtime just to save time during setup. Always prefer exposing `public` or `[SerializeField]` variables so references can be dragged and assigned manually in the Unity Inspector, as it is much more efficient.
