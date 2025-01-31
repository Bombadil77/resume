# Résumé and Code Sample
This repository exists as a resource for a job application to D.O.G.E.
It contains my résumé and a code sample.

My career has been independently developing a timekeeping and billing application named Lucid.
I started in 2003 at the age of 17.
I turned an internship into a product and self-employment.
The application is closed source (though I am happy to review any part of it in real-time).

I have been content but can't not apply for the mission of shrinking bureaucracy.

## Code Sample
For a code sample, I have selected code that tokenizes project names and identifiers for search indexing.
A typical full-text index is not suitable because the data is not natural English.
For example, there are lots of 1 or 2 character sequences that should not be discarded.

I recommend starting with the test cases to visualize the input.
A bite-size function is `MatchParenthetical` in *Parenthetical.cs*.
The entrée is *Tokenizer.cs*.

I selected this code because it is:
- self contained
- algorithm heavy yet approachable (because it is just string parsing)
- shows how I comment code

Lucid at large is mostly a CRUD app.
It is a well-architected application.
That is what I am proud of.
However, it is difficult to extract a concise and exciting code sample showing maintainability of a CRUD app.
So, here is your string parsing.