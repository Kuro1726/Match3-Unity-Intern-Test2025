# Match3 Unity – Gameplay Rewrite Notes

## Board Layout Inspector Tool

- Board layouts are now stored in a separate BoardLayoutSO asset.
- DefaultBoardLayout.asset contains the current 4 x 6, two-layer, 39-item level and is assigned in gamesettings.asset.
- Each placement exposes Item Type, Layer, and Grid Position in the Inspector.
- Grid positions snap to 0.5 units. Position (0, 0) is the bottom-left board cell.
- The Inspector includes a board preview, list reordering, Snap All, Generate From Game Settings, and Assign To Game Settings actions.
- The Inspector color legend now matches gameplay: Type 1 brown, Type 2 red, Type 3 yellow, Type 4 blue, Type 5 green, Type 6 purple, and Type 7 pink.
- Layer tabs filter both the preview and item list so dense layouts remain readable.
- Add New Layer creates the next half-cell-offset layer, fills it with valid groups of three, and updates BoardLayerCount in Game Settings.
- Remove Layer deletes the selected layer (or the highest layer from All), shifts every higher layer down by one, reapplies the half-cell offset, and updates BoardLayerCount.
- Item positions and layer indices are clamped to the valid board area when edited.
- Adding an item to a full layer is rejected instead of placing it outside the board.
- Existing out-of-bounds items are reported and can be removed with Remove Items Outside Board.
- Same-layer items use a one-cell footprint; positions less than one unit apart on both axes are treated as overlapping and rejected.
- Assign and asset saving are blocked while the layout has validation errors.
- Every Type total across the full board must be divisible by three; per-layer Type counts remain divisible by three for the current Autoplay strategy.
- Validation reports duplicate positions, negative layers, off-grid positions, and layer/type counts that are not divisible by three.
- Runtime blocking is calculated from the authored positions: an overlapping item on any higher layer blocks the lower item.
- If Game Settings has no valid BoardLayoutSO assigned, the previous generated-board implementation remains available as a fallback.

Tài liệu này ghi lại các phần gameplay đã được thay đổi và những hạng mục sẽ triển khai tiếp theo.

## Cập nhật triển khai mới nhất

- Board hiện hỗ trợ nhiều layer lệch nhau nửa ô; mặc định là 2 layer.
- Mỗi item layer trên che tối đa 4 item layer dưới và item bị che không thể được chọn.
- Item bị che được làm tối; trạng thái được cập nhật ngay khi blocker phía trên rời board.
- Input, `AUTOPLAY` và `AUTO LOSE` đều chỉ được phép chọn item đang mở khóa.
- Với board `4 × 6` và 2 layer, layer dưới có 24 item, layer trên có 15 item, tổng cộng 39 item.
- Các button Home đã chuyển hoàn toàn sang GameObject thật trong scene; code không còn clone hoặc đổi vị trí button lúc runtime.
- Khay dưới đã được đổi từ 7 ô thành đúng 5 ô.
- Home screen hiện có ba lựa chọn: `PLAY`, `AUTOPLAY` và `AUTO LOSE`.
- `AUTOPLAY` luôn bắt đầu từ board mới và chọn liên tiếp từng nhóm 3 item cùng loại cho tới khi thắng.
- `AUTO LOSE` luôn bắt đầu từ board mới và chọn 5 item theo giới hạn tối đa 2 item mỗi loại, tạo trạng thái như `2 + 2 + 1` để chắc chắn thua.
- Mỗi thao tác tự động có khoảng chờ 0,5 giây và chỉ diễn ra sau khi animation của thao tác trước đã hoàn tất.
- Input thủ công bị khóa trong khi một chế độ tự động đang chạy.
- Pause/resume giữ nguyên kế hoạch tự động và không tạo thêm thao tác chồng lặp.

## Gameplay hiện tại

Project đã được chuyển từ cơ chế match-3 kéo đổi vị trí sang cơ chế chọn item và ghép ba trong khay:

1. Người chơi chạm vào một item trên board để chuyển item đó xuống khay phía dưới.
2. Item đã xuống khay không thể quay lại board.
3. Khi trong khay có đúng 3 item cùng loại, ba item đó được xóa và các item còn lại được dồn lại.
4. Người chơi thắng khi toàn bộ item trên board và trong khay đã được xóa.
5. Người chơi thua nếu toàn bộ ô trong khay bị lấp đầy mà không tạo được bộ ba.

## Các phần đã thực hiện

- Thay thao tác kéo đổi hai item bằng thao tác chạm để đưa một item xuống khay.
- Tạo khay dưới bằng các cell sinh động lúc bắt đầu level.
- Ngăn item trong khay được chọn để đưa trở lại board.
- Kiểm tra và xóa đúng 3 item cùng loại ngay sau mỗi lượt chọn.
- Dồn các item còn lại trong khay sau khi một bộ ba bị xóa.
- Sinh board theo từng nhóm 3 item cùng loại và xáo trộn vị trí. Vì vậy số lượng của mỗi loại item được sử dụng luôn chia hết cho 3.
- Thêm điều kiện thắng và thua độc lập với giới hạn lượt hoặc thời gian cũ.
- Hiển thị đúng popup `LEVEL WIN` khi thắng và `GAME OVER` khi thua.
- Cập nhật HUD để hiển thị số item trong khay và số item còn lại trên board.
- Giữ trạng thái bận khi pause giữa một animation, tránh nhận nhiều thao tác chồng lên nhau.
- Sửa lỗi nút Timer không được gán trong Home screen gây `NullReferenceException`.
- Giữ nguyên các thay đổi có sẵn của project trong 7 prefab normal item.

### Các file chính đã thay đổi

- `Assets/Scripts/Board/Board.cs`
- `Assets/Scripts/Board/Cell.cs`
- `Assets/Scripts/Board/Item.cs`
- `Assets/Scripts/Controllers/BoardController.cs`
- `Assets/Scripts/Controllers/GameManager.cs`
- `Assets/Scripts/GameSettings.cs`
- `Assets/Resources/gamesettings.asset`
- `Assets/Scripts/UI/UIMainManager.cs`
- `Assets/Scripts/UI/UIPanelGame.cs`
- `Assets/Scripts/UI/UIPanelGameOver.cs`
- `Assets/Scripts/UI/UIPanelMain.cs`

## Trạng thái kiểm tra

- Mã runtime đã được biên dịch thành công bằng compiler và các assembly của Unity 2020.3.38f1.
- Chưa thực hiện kiểm thử Play Mode tự động vì project đang mở trong Unity và Unity batch process riêng không có license hợp lệ.
- Cần kiểm tra trực quan animation, kích thước khay và các popup trong Unity Play Mode.

## Kế hoạch yêu cầu tiếp theo (đã triển khai)

### 1. Khay 5 ô

- Đổi số ô dưới khay từ 7 thành đúng 5.
- Cập nhật HUD và bố cục để khay 5 ô nằm cân giữa màn hình.
- Kiểm tra điều kiện thua sau khi xử lý match, để trường hợp item thứ năm tạo thành bộ ba không bị xử thua sai.

### 2. Màn hình kết quả

- Giữ hoặc đơn giản hóa màn hình thắng với nội dung rõ ràng.
- Giữ hoặc đơn giản hóa màn hình thua với nội dung rõ ràng.
- Đảm bảo mỗi kết quả chỉ hiển thị đúng một màn hình.

### 3. Home screen

Home screen sẽ có ba lựa chọn:

- `Play`: chơi thủ công.
- `Autoplay`: tự động chơi để thắng.
- `Auto Lose`: tự động chơi để thua.

### 4. Autoplay thắng

- Khi nhấn `Autoplay`, game bắt đầu từ một board mới hợp lệ.
- Hệ thống lập kế hoạch chọn trọn từng nhóm 3 item cùng loại.
- Mỗi lần chọn item cách nhau 0,5 giây.
- Không thực hiện lượt tiếp theo khi animation hoặc quá trình xóa/dồn khay chưa hoàn tất.
- Chế độ tiếp tục cho tới khi board và khay trống, sau đó hiển thị màn hình thắng.

### 5. Auto Lose

- Khi nhấn `Auto Lose`, game bắt đầu từ một board mới hợp lệ.
- Hệ thống chọn item sao cho không loại nào đạt đủ 3 item trong khay.
- Mỗi lần chọn item cách nhau 0,5 giây.
- Mục tiêu là tạo phân bố như `2 + 2 + 1` hoặc một phân bố tương đương để lấp đầy 5 ô mà không tạo match.
- Board mới phải có đủ loại item để bảo đảm có thể tạo trạng thái thua này.
- Khi đủ 5 ô, game hiển thị màn hình thua.

## Quy tắc cho trạng thái bắt buộc thắng hoặc bắt buộc thua

Các nút tự động trên Home screen sẽ luôn bắt đầu một level mới, thay vì tiếp tục một ván thủ công đang chơi dở. Trước khi chạy, hệ thống sẽ kiểm tra board có đạt được kết quả mong muốn hay không.

- Nếu `Autoplay` không còn đường thắng từ trạng thái hiện tại, level sẽ được reset hoặc sinh lại rồi mới bắt đầu autoplay.
- Nếu `Auto Lose` không thể lấp đầy 5 ô mà vẫn tránh một bộ ba, level sẽ được reset hoặc sinh lại với đủ loại item.
- Nếu sau này cho phép bật chế độ tự động giữa một ván đang chơi, hệ thống phải phân tích trạng thái trước; khi kết quả yêu cầu không còn khả thi, nó sẽ khởi động lại level thay vì chạy sai mục tiêu.

Nhờ quy tắc này, `Autoplay` luôn kết thúc bằng chiến thắng và `Auto Lose` luôn kết thúc bằng thất bại, không phụ thuộc vào một trạng thái dở dang không phù hợp.

## Kiểm thử dự kiến

- Xác nhận số lượng của từng loại item ban đầu chia hết cho 3.
- Xác nhận khay luôn có đúng 5 cell.
- Xác nhận lượt thứ năm tạo match được xử lý match trước khi kiểm tra thua.
- Xác nhận thắng khi board và khay đều trống.
- Xác nhận thua khi 5 ô đầy và không có match.
- Xác nhận Autoplay luôn thắng với delay 0,5 giây giữa các lần chọn.
- Xác nhận Auto Lose luôn thua với delay 0,5 giây giữa các lần chọn.
- Xác nhận pause/resume không làm phát sinh thao tác tự động trùng lặp.
