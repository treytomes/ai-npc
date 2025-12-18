# Facts

Due to the nature of the rules engine, it's important that facts are reference objects.

If you convert these to value object, the engine will complain if multiple rules of the same type show up in the fact list.
