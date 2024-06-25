# Required Tests

## Basecalls
### Basic overwritten Methods:
* virtual method without basecall &rarr; ❌
* abstract method without basecall &rarr; ✔

### If & elses in overwritten virtual Methods:
* if/else/else if &rarr; Given a basecall in one branch, all branches must have a basecall 
* if wihtout else &rarr; Every path through a method must have exactly one basecall
* if without else &rarr; Given a return statement in the if-body, each path through the method must still have excatly one basecall

### Other errors:
* basecalls on other Methods &rarr; ❌
* loops with Basecall &rarr; ❌ (don't forget different loop types)
* basecall in anonymous methods &rarr; ❌
* basecall in local function &rarr; ❌

### Extra case (very unlikely to happen): 
* overwritten virtual method with basecall in a switch expression &rarr; either an error overall OR given a basecall in one branch, all branches must feature a basecall 

## Mixins Nextcalls
Mixins are classes which derive from other classes, where the derivation happens through reflection (feature of re-motion).

Normal (non Mixin case):<BR>
```
public class Parent {
    public virtual void Method() {...}
}

public class Derivation : Parent {
    public override void Method () { base.Method(); ... }
}
```

Mixins:<br>
```
public interface Parent {
    void Method();
}

public class Derivation : Mixins<..., Parent> {
    [OverrideTarget]
    public void Method () { Next.Method(); ... }
}
```

We need to check the exact same things as above, but for mixed methods. We call the base of a mixed method by calling `Next.TheMethodWeAreCurrentlyIn()`.
We can check whether a method has been mixed by checking for the `OverrideTarget` attribute. 

This attribute should only be available on classes which implement some form of `Mixins<...,...>`, but we don't need to check for this, 
as it would only matter if we care about checking whether the Mixin is valid, not whether the basecall was made. 