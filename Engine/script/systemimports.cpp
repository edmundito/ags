//=============================================================================
//
// Adventure Game Studio (AGS)
//
// Copyright (C) 1999-2011 Chris Jones and 2011-2024 various contributors
// The full list of copyright holders can be found in the Copyright.txt
// file, which is part of this source code distribution.
//
// The AGS source code is provided under the Artistic License 2.0.
// A copy of this license can be found in the file License.txt and at
// https://opensource.org/license/artistic-2-0/
//
//=============================================================================
#include <stdlib.h>
#include <string.h>
#include "script/systemimports.h"

SystemImports simp;
SystemImports simp_for_plugin;

uint32_t SystemImports::add(const String &name, const RuntimeScriptValue &value, ccInstance *anotherscr, ScriptValueHint val_hint)
{
    uint32_t ixof = get_index_of(name);
    // Check if symbol already exists
    if (ixof != UINT32_MAX)
    {
        // Only allow override if not a script-exported function
        if (anotherscr == nullptr)
        {
            imports[ixof] = ScriptImport(name, value, anotherscr, val_hint);
        }
        return ixof;
    }

    ixof = imports.size();
    for (size_t i = 0; i < imports.size(); ++i)
    {
        if (imports[i].Name == nullptr)
        {
            ixof = i;
            break;
        }
    }

    btree[name] = ixof;
    if (ixof == imports.size())
        imports.emplace_back(ScriptImport(name, value, anotherscr, val_hint));
    else
        imports[ixof] = ScriptImport(name, value, anotherscr, val_hint);
    return ixof;
}

void SystemImports::remove(const String &name)
{
    uint32_t idx = get_index_of(name);
    if (idx == UINT32_MAX)
        return;
    btree.erase(imports[idx].Name);
    imports[idx] = {};
}

const ScriptImport *SystemImports::getByName(const String &name)
{
    uint32_t o = get_index_of(name);
    if (o == UINT32_MAX)
        return nullptr;

    return &imports[o];
}

const ScriptImport *SystemImports::getByIndex(uint32_t index)
{
    if (index >= imports.size())
        return nullptr;

    return &imports[index];
}

uint32_t SystemImports::get_index_of(const String &name)
{
    // Import names may be commonly formed as "name^N" or "type::name^N",
    // where "type" is a name of a type, "name" is a name of a function,
    // and "N" is a number of arguments.
    
    // First look for the identical name match: this may be the case if:
    // * compiler did not bother to append number of arguments;
    // * we registered a function with exact matching arg number;
    // * we called get_index_of() recursively with arg num cut off (see below).
    IndexMap::const_iterator it = btree.find(name);
    if (it != btree.end())
        return it->second;

    // Next try if the given name contains '^' separator,
    // in which case cut the leftmost section and try it alone.
    if (name.GetLength() > 3)
    {
        size_t c = name.FindCharReverse('^');
        if (c != String::NoIndex && (c >= name.GetLength() - 4))
        {
            return get_index_of(name.Left(c));
        }
    }

    // Finally look for the exported script functions, these commonly are registered
    // as "name$N", where N is a number of arguments.
    String export_name = String::FromFormat("%s$", name.GetCStr());
    it = btree.lower_bound(export_name);
    if (it != btree.end() && it->first.CompareLeft(export_name) == 0)
        return it->second;

    // Not found...
    return UINT32_MAX;
}

String SystemImports::findName(const RuntimeScriptValue &value)
{
    for (const auto &import : imports)
    {
        if (import.Value == value)
        {
            return import.Name;
        }
    }
    return String();
}

void SystemImports::RemoveScriptExports(ccInstance *inst)
{
    if (!inst)
    {
        return;
    }

    for (auto &import : imports)
    {
        if (import.Name == nullptr)
            continue;

        if (import.InstancePtr == inst)
        {
            btree.erase(import.Name);
            import.Name = nullptr;
            import.Value.Invalidate();
            import.InstancePtr = nullptr;
        }
    }
}

void SystemImports::clear()
{
    btree.clear();
    imports.clear();
}
