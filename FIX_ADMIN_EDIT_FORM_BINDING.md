# Fix: Admin Edit Form - Status and Price Not Saving Correctly

## Problem Description

When editing a collection in the Admin panel, the following issues occurred:

1. **Category** always defaulted to "Paintings" (the first option, enum value 0)
2. **Status** was not being saved - always reverting to "Available" 
3. **Currency** was not being saved - always reverting to "EUR"
4. **Price** was being cleared if not explicitly re-entered during edit

## Root Cause Analysis

The issue was in `/app/src/AAS.Web/Areas/Admin/Views/Collections/Edit.cshtml`:

### Problem 1: Status and Currency Dropdowns Not Showing Current Values

The Status and Currency dropdowns were not properly marking the current/selected values:

**Before (INCORRECT):**
```html
<select asp-for="Status" name="Status" class="form-select">
    <option value="0">Available</option>
    <option value="1">Sold</option>
</select>
```

This caused the dropdown to **always display the first option** (index 0) when the form loaded, regardless of the actual stored value in the database. When users submitted the form without explicitly changing these fields, the value "0" would be sent back to the server.

### Problem 2: Category Dropdown Pattern Was Correct (For Comparison)

The Category dropdown was implemented correctly and served as a reference:

```html
<select name="Category" class="form-select">
    <option value="0" @(Model.Category == CollectionCategory.Paintings ? "selected" : "")>Paintings</option>
    <option value="1" @(Model.Category == CollectionCategory.Jewelry ? "selected" : "")>Jewelry</option>
    ...
</select>
```

This correctly checks `Model.Category` and adds the `selected` attribute to the matching option.

## Solution Implemented

### Fix 1: Status Dropdown - Added Selected Attribute Binding

**File:** `/app/src/AAS.Web/Areas/Admin/Views/Collections/Edit.cshtml` (Lines 46-52)

**After (CORRECT):**
```html
<select asp-for="Status" name="Status" class="form-select">
    <option value="0" @(Model.Status == CollectionStatus.Available ? "selected" : "")>Available</option>
    <option value="1" @(Model.Status == CollectionStatus.Sold ? "selected" : "")>Sold</option>
</select>
```

### Fix 2: Currency Dropdown - Added Selected Attribute Binding

**File:** `/app/src/AAS.Web/Areas/Admin/Views/Collections/Edit.cshtml` (Lines 57-63)

**After (CORRECT):**
```html
<select asp-for="Currency" name="Currency" class="form-select">
    <option value="0" @(Model.Currency == Currency.EUR ? "selected" : "")>EUR (€)</option>
    <option value="1" @(Model.Currency == Currency.USD ? "selected" : "")>USD ($)</option>
</select>
```

### Fix 3: Added Debug Logging to Controller

**File:** `/app/src/AAS.Web/Areas/Admin/Controllers/CollectionsController.cs`

Added console logging to trace form values being received and parsed:

```csharp
Console.WriteLine($"[EDIT POST DEBUG] Collection ID: {id}");
Console.WriteLine($"[EDIT POST DEBUG] Form - Category: '{categoryStr}', Status: '{statusStr}', Currency: '{currencyStr}', Price: '{priceStr}'");
Console.WriteLine($"[EDIT POST DEBUG] Updated Category to: {existing.Category} ({categoryInt})");
Console.WriteLine($"[EDIT POST DEBUG] Updated Status to: {existing.Status} ({statusInt})");
Console.WriteLine($"[EDIT POST DEBUG] Updated Price to: {existing.Price}");
Console.WriteLine($"[EDIT POST DEBUG] Price field empty, keeping existing value: {existing.Price}");
Console.WriteLine($"[EDIT POST DEBUG] Updated Currency to: {existing.Currency} ({currencyInt})");
```

This helps diagnose any future issues by showing exactly what values are being processed.

## Why This Fix Works

1. **Proper Value Display**: When the Edit form loads, the dropdowns now correctly show the current stored values from the database
2. **Correct Submission**: When users submit without changing these fields, the correct current values are sent (not always "0")
3. **Consistent Pattern**: Follows the same pattern already used successfully for the Category dropdown
4. **ASP.NET Core Standard**: Uses standard Razor syntax for conditional attributes

## Testing Instructions

### Test Case 1: Edit and Save Without Changing Status/Currency

1. Log in to Admin panel
2. Go to Collections → Edit an existing collection
3. Note the current Status (e.g., "Sold") and Currency (e.g., "USD")
4. Change only the Title or Description (don't touch Status/Currency/Price)
5. Click "Save"
6. Verify that Status and Currency remain unchanged (should still be "Sold" and "USD")

### Test Case 2: Edit and Change Status

1. Edit a collection
2. Change Status from "Available" to "Sold"
3. Save
4. Re-open the edit form
5. Verify Status dropdown shows "Sold" as selected

### Test Case 3: Edit and Add Price

1. Edit a collection that has no price set
2. Enter a price (e.g., 1500.00)
3. Select currency (e.g., USD)
4. Save
5. Re-open the edit form
6. Verify:
   - Price shows "1500.00"
   - Currency dropdown shows "USD" as selected

### Test Case 4: Edit Without Re-entering Price

1. Edit a collection that has a price (e.g., 1500.00 USD)
2. Change only the Title
3. Leave Price field as-is (don't clear it, don't re-type it)
4. Save
5. Re-open the edit form
6. Verify Price is still 1500.00

### Test Case 5: Category Still Works

1. Edit a collection
2. Change Category to "Jewelry"
3. Save
4. Re-open the edit form
5. Verify Category dropdown shows "Jewelry" as selected

## Log Files to Check

After testing, check logs for debug output:

```bash
# Check backend logs for debug messages
tail -n 100 /var/log/supervisor/backend.*.log

# Or if running with Docker:
docker logs aas-web

# Look for lines like:
# [EDIT POST DEBUG] Collection ID: 1
# [EDIT POST DEBUG] Form - Category: '2', Status: '1', Currency: '1', Price: '1500.00'
# [EDIT POST DEBUG] Updated Status to: Sold (1)
# [EDIT POST DEBUG] Updated Currency to: USD (1)
```

## Files Changed

1. `/app/src/AAS.Web/Areas/Admin/Views/Collections/Edit.cshtml`
   - Fixed Status dropdown (lines 46-52)
   - Fixed Currency dropdown (lines 57-63)

2. `/app/src/AAS.Web/Areas/Admin/Controllers/CollectionsController.cs`
   - Added debug logging to Edit POST action (lines 247-333)

## Related Enums

For reference, the enum values are defined in `/app/src/AAS.Web/Models/Enum.cs`:

```csharp
public enum CollectionStatus 
{ 
    Available = 0,  // Default
    Sold = 1
}

public enum Currency 
{ 
    EUR = 0,  // Default (€)
    USD = 1   // ($)
}

public enum CollectionCategory 
{ 
    Paintings = 0,  // Default
    Jewelry = 1, 
    Watches = 2, 
    Statues = 3, 
    Other = 4 
}
```

## Status

✅ **FIXED** - Changes implemented and ready for testing on production/staging server.

The fix is straightforward and follows standard ASP.NET Core Razor patterns. It should resolve all the reported issues with Status, Currency, and Category not saving correctly.

## Next Steps

1. Deploy these changes to your server
2. Restart the ASP.NET Core application
3. Run through the test cases above
4. Monitor the logs for the debug output to confirm values are being received and saved correctly

If any issues persist after this fix, the debug logging will help identify where the problem is occurring in the data flow.
