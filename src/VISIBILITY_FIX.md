# Visibility fix 1.3.1

Reproduced on unmodified 1.3 scene: load with all nodes hidden, show all, Surface.Geometry remains null. New visibility tests fail before the patch and pass after it.

Initialise frozen geometry independently of visibility; track display invalidation when detached/reattached; refresh pose and clipping when returning to the scene. Avoid removing native TreeNodes during AfterCheck. Keep checkbox synchronisation guarded. Ignore native checkbox WM_LBUTTONDBLCLK, preserving label double-click focus. Clear pending housing flags when changing transmission kind.

Upstream checkbox issue: https://github.com/dotnet/winforms/issues/7063

Validation: 310 targeted assertions across four kinds, 30 cycles each, pixel-identical restoration, all section axes, exploded view, asynchronous load/recalculation; 389 existing UI checks; full viewer tests. Native mouse tested toolbar and repeated checkbox double-clicks. CAD geometry, mesh generation and export format implementations are unchanged.
