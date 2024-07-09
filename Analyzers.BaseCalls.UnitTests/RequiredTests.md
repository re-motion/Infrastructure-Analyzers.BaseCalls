# Required Tests

## BaseCallCheck attribute

The attribute `BaseCallCheck` should be applied on methods to check. More info about when
the methods should be checked can be found in the `BaseCallCheckAttribute.cs` file.

Since the attribute should be used in the code that is analyzed, we somehow need to get the attribute to users.
There are 2 options:

* [Option 1] The attribute is only identified by the namespace and its name. This allows us to create a simple nuget
  package only containing the 2 attributes and the enum.
  If people don't want to use the nuget package, they can add the same attributes and enum in the same namespace, and it
  should just work.
* [Option 2] This is more complicated than option 1.
  Read [this](https://andrewlock.net/creating-a-source-generator-part-7-solving-the-source-generator-marker-attribute-problem-part1/)
  and its second part,
  and implement it. This would make the analyzer easier to use and also allow for better checks in the analyzer of the
  attributes.

## Basecalls

### Basic overwritten Methods:

* virtual method without basecall &rarr; ❌
* abstract method without basecall &rarr; ✔

### If & elses in overwritten virtual Methods:

* if/else/else if &rarr; Given a basecall in one branch, all branches must have a basecall
* if wihtout else &rarr; Every path through a method must have exactly one basecall
* if without else &rarr; Given a return statement in the if-body, each path through the method must still have excatly
  one basecall

### Other errors:

* basecalls on other Methods &rarr; ❌
* loops with Basecall &rarr; ❌ (don't forget different loop types)
* basecall in anonymous methods &rarr; ❌
* basecall in local function &rarr; ❌
* basecall in method which does not override &rarr; ❌

### Extra case (very unlikely to happen):

* overwritten virtual method with basecall in a switch expression &rarr; either an error overall OR given a basecall in
  one branch, all branches must feature a basecall

## Mixins Nextcalls

Mixins are classes which derive from other classes, where the derivation happens through reflection (feature of
re-motion).

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

public class Derivation : Mixin<..., Parent> {
    [OverrideTarget]
    public void Method () { Next.Method(); ... }
}
```

We need to check the exact same things as above, but for mixed methods. We call the base of a mixed method by
calling `Next.TheMethodWeAreCurrentlyIn()`.
We can check whether a method has been mixed by checking for the `OverrideTarget` attribute.

This attribute should only be available on classes which implement some form of `Mixins<...,...>`, but we don't need to
check for this,
as it would only matter if we care about checking whether the Mixin is valid, not whether the basecall was made. 