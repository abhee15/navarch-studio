# Page snapshot

```yaml
- generic [ref=e6]:
  - generic [ref=e8]:
    - generic [ref=e9]:
      - img [ref=e12]
      - heading "Welcome back" [level=3] [ref=e15]
      - paragraph [ref=e16]: Sign in to your account to continue
    - generic [ref=e18]:
      - generic [ref=e19]:
        - text: Email
        - generic [ref=e20]:
          - img [ref=e21]
          - textbox "Email" [ref=e24]:
            - /placeholder: you@example.com
      - generic [ref=e25]:
        - text: Password
        - generic [ref=e26]:
          - img [ref=e27]
          - textbox "Password" [ref=e30]:
            - /placeholder: Enter your password
      - button "Sign in" [ref=e31] [cursor=pointer]:
        - img [ref=e32]
        - text: Sign in
      - generic [ref=e39]: New to our platform?
      - link "Create an account →" [ref=e41] [cursor=pointer]:
        - /url: /signup
  - contentinfo [ref=e42]:
    - paragraph [ref=e44]: © 2025 NavArch Studio. All rights reserved.
```