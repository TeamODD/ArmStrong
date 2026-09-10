병원 출입 단말기 오브젝트 V2
===========================

이 파일은 "오브젝트만" 만드는 버전입니다.
기능 코드(비밀번호 판정, 카드 인식, 문 열기)는 넣지 않았습니다.

포함 내용
---------
- 더 깔끔한 단말기 외형
- 낡은 병원 금속 느낌 텍스처
- 앞면 장식용 오버레이 텍스처
- 위/아래 판 포함
- AC-1047 번호판 느낌
- 화면, 카드 슬롯, LED, 키패드 버튼 분리
- 버튼(Key_1 ~ Key_9, Key_Star, Key_0, Key_Hash) 별도 오브젝트
- 나중에 스크립트 붙이기 좋게 Hook 빈 오브젝트 포함

사용 방법
---------
1. ZIP을 풀고 HospitalAccessReader_ObjectOnly_V2 폴더를 Unity 프로젝트 Assets 안에 넣기
2. Unity 컴파일 완료 대기
3. 메뉴에서:
   Tools > Hospital Horror > Create Pretty Access Reader Object
4. 생성 위치:
   Assets/HospitalReaderObjectGenerated/Hospital_Access_Reader_Object.prefab

메모
----
- 카드 관련 기능은 일부러 안 넣음
- 키패드 버튼과 카드 슬롯에는 Collider를 남겨둠
- 화면/키패드/카드 슬롯 위치 찾기 쉽도록 _HOOKS 오브젝트 있음
