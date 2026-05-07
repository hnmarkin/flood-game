# Flood Game Agentic Coding Guidelines

## Event Subscription Rules

1. The subscriber is responsible for unsubscribing from events it subscribes to.
2. Subscribe in `OnEnable()` and unsubscribe in `OnDisable()` by default.
3. Use `OnDestroy()` only for listeners that must stay subscribed while disabled.
4. Avoid anonymous lambdas for event subscriptions unless the delegate is stored and can be unsubscribed.
5. Static events and global event buses must provide an explicit cleanup/reset path.
6. Events do not own subscribers and must not destroy listener objects.
