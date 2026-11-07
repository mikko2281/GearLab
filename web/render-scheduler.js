// Render changed views and damping frames; a settled scene does no GPU work.
export function createRenderScheduler(render, requestFrame, visible = () => true) {
  let pending = false;
  function invalidate() {
    if (pending || !visible()) return;
    pending = true;
    requestFrame(time => {
      pending = false;
      if (!visible()) return;
      if (render(time)) invalidate();
    });
  }
  return { invalidate, get pending() { return pending; } };
}
