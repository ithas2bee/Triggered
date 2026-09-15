# Repository Structure

US-001 established this structure and future work must preserve it:

```text
Triggered/
├── apps/
│   ├── api/src/{Domain,Application,Infrastructure,Presentation}
│   │   └── tests/
│   ├── web/src/{presentation,application,infrastructure}
│   │   └── tests/
│   └── mobile/src/{presentation,application,infrastructure}
│       └── tests/
├── packages/{domain,application,api-client,ui,shared}
├── tests/{integration,end-to-end}
├── docs/{product,architecture,development,decisions}
├── docker/{development,staging,production}
└── .github/workflows/
```

The folders describe intended boundaries; they are not permission to
reorganize the repository. Add files to the established area that owns their
responsibility.

