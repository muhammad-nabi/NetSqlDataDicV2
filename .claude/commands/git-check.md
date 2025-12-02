Review the current Git changes in this ASP.NET web application and determine whether the changes are safe to push to production.

Evaluate the diff for issues in the following prioritized categories:

1. Security (highest priority):
Look for vulnerabilities (e.g., input validation gaps, unsafe deserialization, SQL injection, missing authorization checks, insecure configuration changes, exposure of secrets).

2. Error Handling & Reliability:
Identify missing exception handling, silent failures, improper logging, potential null reference issues, or unhandled edge cases.

3. Performance:
Detect unnecessary allocations, inefficient queries, excessive I/O, blocking calls in async contexts, or any change that may degrade runtime performance.

Summarize any findings with:
Clear explanation of risks
File and line references

Recommended fixes
If no issues are detected, provide a clear statement confirming that the diff appears safe for production deployment.

Output Requirements:
Provide a structured report with sections for Security, Error Handling, and Performance.
Include actionable recommendations for any issue found.
Do not approve the changes unless a full check in all categories passes.
Begin by retrieving and analyzing the current diff.