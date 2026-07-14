import 'package:dio/dio.dart';
import '../common/app_config.dart';
import 'token_storage.dart';

/// ApiClient: bao bọc Dio thành 1 "cổng gọi API" dùng chung cả app.
class ApiClient{
  //dio là đối tượng thực hieejnr request HTTP (GET/POST)
  late final Dio _dio;

  // Cho phép truy cập _dio từ ngoài để gọi .get/.post
  Dio get dio => _dio;

  ApiClient(){
    // BaseOptions: cấu hình mặc định cho mọi request.
    _dio = Dio(
      BaseOptions(
        baseUrl: AppConfig.apiBaseUrl, // gốc URL, sau này chỉ cần ghi '/auth/login'
        connectTimeout: const Duration(seconds: 15), // chờ kết nối tối đa 15s
        receiveTimeout: const Duration(seconds: 15), // chờ phản hồi tối đa 15s
        // Không tự ném lỗi với status < 500 để mình tự xử lý message từ API
        validateStatus: (status) => status != null && status < 500,
      ),
    );

    //Interceptor ="Trạm kiểm soát" chạy trước mỗi requesr/ sau mỗi response (dùng gn token header)
    _dio.interceptors.add(
      InterceptorsWrapper(
        //onRequest: chạy trước khi gửi request đi
        onRequest: (options, handler) async {
          final token = await TokenStorage.getToken();// đọc token đã lưu
          if(token!= null && token.isNotEmpty){
            // Gắn token vào header Authorization theo chuẩn "Bearer <token>"
            options.headers['Authorization'] = 'Bearer $token'; // gắn vào header'
          }
          handler.next(options); // tiếp tục gửi request đi
        },
      ),
    );
  }
}