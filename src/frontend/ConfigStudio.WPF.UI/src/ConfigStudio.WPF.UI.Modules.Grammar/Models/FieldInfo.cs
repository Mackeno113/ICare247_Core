// File    : FieldInfo.cs
// Module  : Grammar
// Layer   : Presentation
// Purpose : Type alias — FieldInfo đã được chuyển sang Core.Data.ExpressionFieldInfo.
//           Giữ lại để tránh breaking change nếu có code cũ reference.

global using FieldInfo = ConfigStudio.WPF.UI.Core.Data.ExpressionFieldInfo;
