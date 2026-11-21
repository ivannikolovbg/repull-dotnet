#!/usr/bin/env python3
"""
Pre-codegen spec transform: writes the spec Kiota generates from.

    python3 scripts/prepare-spec.py IN OUT

openapi/v1.json stays byte-identical to the live spec (the freshness check
compares it); only the copy handed to Kiota is rewritten.

Rule: a oneOf of exactly one object schema (no discriminator) plus primitives
becomes an anyOf. Kiota turns that oneOf into a wrapper whose factory only ever
fills the primitive branch: for an object value (e.g. Vrbo protection
`"damageDeposit": {"amount": 500}`) neither branch is set and the serializer
writes the property name with no value, which is invalid JSON. For an anyOf,
Kiota instantiates the object branch when the value is not a primitive, so
both shapes read and write correctly. The request on the wire is unchanged:
one value, either the number or the object.

Stdlib only. Idempotent.
"""
import json
import sys

PRIMITIVES = {"boolean", "string", "number", "integer"}


def main() -> int:
    if len(sys.argv) != 3:
        print("usage: prepare-spec.py IN OUT", file=sys.stderr)
        return 2
    with open(sys.argv[1], encoding="utf-8") as f:
        spec = json.load(f)
    schemas = spec.get("components", {}).get("schemas", {})

    def resolve(s):
        for _ in range(20):
            if not isinstance(s, dict) or "$ref" not in s:
                break
            s = schemas.get(s["$ref"].rsplit("/", 1)[-1], {})
        return s if isinstance(s, dict) else {}

    def kind(s):
        s = resolve(s)
        if s.get("type") in PRIMITIVES and "properties" not in s:
            return "primitive"
        if s.get("type") == "object" or "properties" in s:
            return "object"
        return "other"

    count = 0

    def walk(node):
        nonlocal count
        if isinstance(node, dict):
            branches = node.get("oneOf")
            if isinstance(branches, list) and "discriminator" not in node:
                kinds = [kind(b) for b in branches]
                if kinds.count("object") == 1 and kinds.count("primitive") == len(kinds) - 1:
                    node["anyOf"] = node.pop("oneOf")
                    count += 1
            for v in node.values():
                walk(v)
        elif isinstance(node, list):
            for v in node:
                walk(v)

    walk(spec)
    with open(sys.argv[2], "w", encoding="utf-8") as f:
        json.dump(spec, f, ensure_ascii=False)
    print(f"prepare-spec: {count} object|primitive oneOf schemas made anyOf", file=sys.stderr)
    return 0


if __name__ == "__main__":
    sys.exit(main())
